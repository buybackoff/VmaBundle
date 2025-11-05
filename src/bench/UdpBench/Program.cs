using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using HdrHistogram;

namespace UdpBench;

internal class Program
{
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    private static Socket? _socket; // shared for disposal-based cancellation
    private static LongHistogram? _histActive; // currently recording
    private static LongHistogram? _histStandby; // idle, will become next active after swap
    private static LongHistogram _histTotal = null!; 

    public static void Main(string[] args)
    {
        // Handle Ctrl+C: cancel operations but keep process alive to allow graceful shutdown
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true; // prevent immediate termination
            if (!_cts.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation requested (Ctrl+C)...");
                _cts.Cancel();
                try
                {
                    _socket?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Socket dispose error: {ex.Message}");
                }
            }
        };

        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        IPEndPoint? endpoint = null;
        long rate = 0;
        bool isServer = false;
        bool isClient = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-s":
                    isServer = true;
                    if (i + 1 < args.Length && IPEndPoint.TryParse(args[i + 1], out endpoint))
                    {
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Invalid endpoint format for server.");
                        PrintUsage();
                        return;
                    }

                    break;
                case "-c":
                    isClient = true;
                    if (i + 1 < args.Length && IPEndPoint.TryParse(args[i + 1], out endpoint))
                    {
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Invalid endpoint format for client.");
                        PrintUsage();
                        return;
                    }

                    break;
                case "-r":
                    if (i + 1 < args.Length && long.TryParse(args[i + 1], out rate))
                    {
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Invalid rate value.");
                        PrintUsage();
                        return;
                    }

                    break;
            }
        }

        if (isServer == isClient)
        {
            Console.WriteLine("You must specify exactly one role: -s (server) or -c (client).");
            PrintUsage();
            return;
        }

        if (endpoint == null)
        {
            Console.WriteLine("Endpoint must be specified.");
            PrintUsage();
            return;
        }

        if (isServer)
        {
            RunServer(endpoint);
        }
        else
        {
            RunClient(endpoint, rate);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  UdpBench -s <ip:port>");
        Console.WriteLine("  UdpBench -c <ip:port> [-r <nanoseconds>]");
    }

    private static void RunServer(IPEndPoint endpoint)
    {
        Console.WriteLine($"Starting server on {endpoint}...");
        _socket = CreateSocket(endpoint.AddressFamily);
        _socket.Bind(endpoint);

        var buffer = new byte[2048];
        var bufferSpan = buffer.AsSpan();
        ref var bufferRef = ref buffer[0];

        uint lastSeq = 0;

        var socketAddress = new SocketAddress(AddressFamily.InterNetwork);

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var length = _socket.ReceiveFrom(buffer, SocketFlags.None, socketAddress);
                if (length == 16)
                {
                    var seq = Unsafe.ReadUnaligned<uint>(ref bufferRef);
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref bufferRef, 16), lastSeq != 0 && seq != lastSeq + 1 ? lastSeq : 0);

                    if (seq != 0)
                        lastSeq = seq;

                    // Send first 20 bytes: original 16 + 4-byte missed-seq indicator
                    _socket.SendTo(bufferSpan.Slice(0, 20), SocketFlags.None, socketAddress);
                }
            }
            catch (ObjectDisposedException)
            {
                break; // expected during cancellation
            }
            catch (SocketException se) when (_cts.IsCancellationRequested)
            {
                break; // cancellation path
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server receive error: {ex.Message}");
            }
        }

        Console.WriteLine("Server exited");
    }

    private static void RunClient(IPEndPoint serverEndpoint, long rate)
    {
        Console.WriteLine($"Starting client, sending to {serverEndpoint}, rate: {rate:N0} ns");
        _socket = CreateSocket(serverEndpoint.AddressFamily);
        _socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.25.134"), serverEndpoint.Port + 1));
        // Initialize dual histograms (max latency ~1ms per user requirement)
        var highestTrackable = Stopwatch.Frequency; // ~1ms worth of ticks
        _histActive = new LongHistogram(highestTrackable, 5);
        _histStandby = new LongHistogram(highestTrackable, 5);
        _histTotal = new LongHistogram(highestTrackable, 5);

        var sender = new Thread(() => Sender(_socket!, serverEndpoint, rate));
        sender.Priority = ThreadPriority.AboveNormal;

        var receiver = new Thread(() => Receiver(_socket!));
        receiver.Priority = ThreadPriority.AboveNormal;

        receiver.Start();
        sender.Start();

        var printTask = Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
                    SwapAndPrint();
                }
            }
            catch (OperationCanceledException) { }
        });

        // Wait for either cancellation (Ctrl+C) or timeout
        _cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(60));
        if (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("Time limit reached, stopping...");
            _cts.Cancel();
            try
            {
                _socket?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Socket dispose error: {ex.Message}");
            }
        }

        sender.Join();
        receiver.Join();
        printTask.Wait();

        Console.WriteLine("Client exited");
    }

    private static Socket CreateSocket(AddressFamily addressFamily)
    {
        var socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);

        if (OperatingSystem.IsWindows())
        {
            const uint IOC_IN = 0x80000000;
            const uint IOC_VENDOR = 0x18000000;
            const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }

        return socket;
    }

    private static void Sender(Socket socket, IPEndPoint serverEndpoint, long rate)
    {
        uint seq = 0;
        var buffer = new byte[16];
        ref var bufferRef = ref buffer[0];
        long ticksPerSend = rate * Stopwatch.Frequency / 1_000_000_000;

        var serverAddress = serverEndpoint.Serialize();

        while (!_cts.IsCancellationRequested)
        {
            long start = Stopwatch.GetTimestamp();

            seq++;

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bufferRef, 0), seq);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bufferRef, 8), start);

            socket.SendTo(buffer, SocketFlags.None, serverAddress);

            if (rate > 0)
            {
                while (Stopwatch.GetTimestamp() - start < ticksPerSend)
                {
                    // Spin
                }
            }
        }

        Console.WriteLine("Client sender loop exited");
    }

    private static void Receiver(Socket socket)
    {
        var buffer = new byte[2048];
        ref var bufferRef = ref buffer[0];

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var length = socket.Receive(buffer, SocketFlags.None);
                if (length == 20)
                {
                    long now = Stopwatch.GetTimestamp();
                    long sent = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref bufferRef, 8));
                    var rtt = now - sent;
                    _histActive?.RecordValue(rtt);
                    _histTotal.RecordValue(rtt);
                }
            }
            catch (ObjectDisposedException)
            {
                break; // expected cancellation path
            }
            catch (SocketException se) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client receive error: {ex.Message}");
            }
        }

        if (_histActive is not null)
        {
            var scalingRatio = OutputScalingFactor.TimeStampToMicroseconds;
            _histActive.OutputPercentileDistribution(
                Console.Out,
                outputValueUnitScalingRatio: scalingRatio);
        }

        Console.WriteLine("Client receiver loop exited");
    }

    private static void PrintPercentiles(LongHistogram histogram)
    {
        if (histogram.TotalCount == 0)
            return;

        var p10 = histogram.GetValueAtPercentile(10);
        var p25 = histogram.GetValueAtPercentile(25);
        var p50 = histogram.GetValueAtPercentile(50);
        var p75 = histogram.GetValueAtPercentile(75);
        var p90 = histogram.GetValueAtPercentile(90);
        var p99 = histogram.GetValueAtPercentile(99);
        var p999 = histogram.GetValueAtPercentile(99.9);
        var min = histogram.GetValueAtPercentile(1); // TODO
        var max = histogram.GetMaxValue();

        var p10Us = TicksToUs(p10);
        var p25Us = TicksToUs(p25);
        var p50Us = TicksToUs(p50);
        var p75Us = TicksToUs(p75);
        var p90Us = TicksToUs(p90);
        var p99Us = TicksToUs(p99);
        var p999Us = TicksToUs(p999);
        var minUs = TicksToUs(min);
        var maxUs = TicksToUs(max);

        // Microsecond output with 2 decimals
        Console.WriteLine(
            $"Min: {minUs:N2}µs, P10: {p10Us:N2}µs, P25: {p25Us:N2}µs, P50: {p50Us:N2}µs, P75: {p75Us:N2}µs, P90: {p90Us:N2}µs, P99: {p99Us:N2}µs, P99.9: {p999Us:N2}µs, Max: {maxUs:N2}µs, Count: {histogram.TotalCount}");
    }

    // Atomically swap active and standby histograms, print previous active, then reset it for reuse
    private static void SwapAndPrint()
    {
        if (_histActive is null || _histStandby is null)
            return;

        var previousActive = Interlocked.Exchange(ref _histActive, _histStandby);
        // Now _histActive points to old standby; previousActive holds data
        PrintPercentiles(previousActive);
        previousActive.Reset();
        // Put reset histogram back as standby
        _histStandby = previousActive;
    }

    // Convert stopwatch ticks to whole nanoseconds (integer). Fractional nanoseconds are truncated.
    private static long TicksToNs(long ticks) => ticks * 1_000_000_000L / Stopwatch.Frequency;
    private static double TicksToUs(long ticks) => (double)ticks * 1_000_000.0 / Stopwatch.Frequency;
}