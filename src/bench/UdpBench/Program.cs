using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using HdrHistogram;

namespace UdpBench;

internal class Program
{
    private static CancellationTokenSource _cts = new CancellationTokenSource();

    public static void Main(string[] args)
    {
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
        using var socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endpoint);

        var buffer = new byte[2048];
        var bufferSpan = buffer.AsSpan();
        ref var bufferRef = ref buffer[0];

        uint lastSeq = 0;

        var socketAddress = new SocketAddress(AddressFamily.InterNetwork);

        while (!_cts.IsCancellationRequested)
        {
            var length = socket.ReceiveFrom(buffer, SocketFlags.None, socketAddress);
            if (length == 16)
            {
                var seq = Unsafe.ReadUnaligned<uint>(ref bufferRef);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref bufferRef, 16), lastSeq != 0 && seq != lastSeq + 1 ? lastSeq : 0);

                if (seq != 0)
                    lastSeq = seq;

                // Send first 20 bytes: original 16 + 4-byte missed-seq indicator
                socket.SendTo(bufferSpan.Slice(0, 20), SocketFlags.None, socketAddress);
            }
        }
    }

    private static void RunClient(IPEndPoint serverEndpoint, long rate)
    {
        Console.WriteLine($"Starting client, connecting to {serverEndpoint}, rate: {rate} ns");

        using var socket = new Socket(serverEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        socket.Bind(new IPEndPoint(IPAddress.Any, serverEndpoint.Port));

        var histogram = new LongHistogram(Stopwatch.Frequency / 1000, 5);

        var sender = new Thread(() => Sender(socket, serverEndpoint, rate));
        var receiver = new Thread(() => Receiver(socket, histogram));

        var printTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, _cts.Token);
                PrintPercentiles(histogram);
                histogram.Reset();
            }
        });

        Thread.Sleep(TimeSpan.FromSeconds(30));
        _cts.Cancel();
        sender.Join();
        receiver.Join();
        printTask.Wait();
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
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref bufferRef, 8), Stopwatch.GetTimestamp());

            socket.SendTo(buffer, SocketFlags.None, serverAddress);

            if (rate > 0)
            {
                while (Stopwatch.GetTimestamp() - start < ticksPerSend)
                {
                    // Spin
                }
            }
        }

        Console.WriteLine("Sender loop exited");
    }

    private static void Receiver(Socket socket, LongHistogram histogram)
    {
        var buffer = new byte[2048];
        ref var bufferRef = ref buffer[0];

        while (!_cts.IsCancellationRequested)
        {
            var length = socket.Receive(buffer, SocketFlags.None);
            if (length == 20)
            {
                long now = Stopwatch.GetTimestamp();
                long sent = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref bufferRef, 8));
                var rtt = now - sent;
                histogram.RecordValue(rtt);
            }
        }

        var scalingRatio = OutputScalingFactor.TimeStampToMicroseconds;
        histogram.OutputPercentileDistribution(
            Console.Out,
            outputValueUnitScalingRatio: scalingRatio);
    }

    private static void PrintPercentiles(LongHistogram histogram)
    {
        var p10 = histogram.GetValueAtPercentile(10);
        var p25 = histogram.GetValueAtPercentile(25);
        var p50 = histogram.GetValueAtPercentile(50);
        var p75 = histogram.GetValueAtPercentile(75);
        var p90 = histogram.GetValueAtPercentile(90);
        var p99 = histogram.GetValueAtPercentile(99);
        var p999 = histogram.GetValueAtPercentile(99.9);
        var min = histogram.GetMinNonZeroValue();
        var max = histogram.GetMaxValue();

        var p10Ns = TicksToNs(p10);
        var p25Ns = TicksToNs(p25);
        var p50Ns = TicksToNs(p50);
        var p75Ns = TicksToNs(p75);
        var p90Ns = TicksToNs(p90);
        var p99Ns = TicksToNs(p99);
        var p999Ns = TicksToNs(p999);
        var minNs = TicksToNs(min);
        var maxNs = TicksToNs(max);

        // Nanosecond output, integer formatting with separators, per requirements
        Console.WriteLine(
            $"Min: {minNs:N0}ns, P10: {p10Ns:N0}ns, P25: {p25Ns:N0}ns, P50: {p50Ns:N0}ns, P75: {p75Ns:N0}ns, P90: {p90Ns:N0}ns, P99: {p99Ns:N0}ns, P99.9: {p999Ns:N0}ns, Max: {maxNs:N0}ns, Count: {histogram.TotalCount}");
    }

    // Convert stopwatch ticks to whole nanoseconds (integer). Fractional nanoseconds are truncated.
    private static long TicksToNs(long ticks) => ticks * 1_000_000_000L / Stopwatch.Frequency;
}