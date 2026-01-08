using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VmaBundle;

public static class VmaHelpers
{
    private static bool CheckRawSocketsCapability()
    {
        // This method attempts to send a minimal ICMP echo request using a raw socket.
        // On Linux this requires CAP_NET_RAW; without it, the Socket operations will
        // fail with a permission error. We report success/failure but do not throw.

        const string targetHost = "127.0.0.1";

        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            var ipAddress = IPAddress.Parse(targetHost);
            var endPoint = new IPEndPoint(ipAddress, 0);

            // Minimal ICMP Echo Request packet (Type 8, Code 0). For this check we
            // only care whether we are allowed to send via a raw socket, not about
            // having a fully correct ICMP implementation.
            var packet = new byte[8];
            packet[0] = 8; // Type: Echo Request
            packet[1] = 0; // Code

            socket.SendTo(packet, endPoint);

            return true;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private static bool? _hasRawSocketsCapability;

    /// <summary>
    /// VMA requires CAP_NET_RAW.
    /// </summary>
    public static bool HasRawSocketsCapability => _hasRawSocketsCapability ??= CheckRawSocketsCapability();

    /// <summary>
    /// Detects the Linux distribution and returns the distro identifier used by VmaBundle packages.
    /// Checks /etc/os-release and /usr/lib/os-release for distribution information.
    /// </summary>
    /// <returns>
    /// One of: "debian13", "debian12", "ubuntu2404", "ubuntu2510" if matched, otherwise null.
    /// </returns>
    public static string? DetectLinuxDistro()
    {
        if (!OperatingSystem.IsLinux())
            return null;

        var osReleasePaths = new[] { "/etc/os-release", "/usr/lib/os-release" };

        foreach (var path in osReleasePaths)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var content = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                // Check for Debian
                if (content.Contains("NAME=\"Debian"))
                {
                    if (content.Contains("VERSION_ID=\"13"))
                        return "debian13";

                    if (content.Contains("VERSION_ID=\"12"))
                        return "debian12";
                }

                // Check for Ubuntu
                if (content.Contains("NAME=\"Ubuntu"))
                {
                    if (content.Contains("VERSION_ID=\"24.04"))
                        return "ubuntu2404";

                    if (content.Contains("VERSION_ID=\"25.10"))
                        return "ubuntu2510";
                }
            }
            catch
            {
                // If we can't read the file, try the next path
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the path to libvma.so for the current Linux distribution.
    /// The library is expected to be in vmabundle\{distro}\lib\libvma.so relative to this assembly's location.
    /// </summary>
    /// <returns>
    /// The full path to libvma.so if found, otherwise null.
    /// </returns>
    public static string? GetLibVmaPath()
    {
        var distro = DetectLinuxDistro();
        if (distro == null)
            return null;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrWhiteSpace(assemblyLocation))
            return null;

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (assemblyDir == null)
            return null;

        var libVmaPath = Path.Combine(assemblyDir, "vmabundle", distro, "lib", "libvma.so");

        return File.Exists(libVmaPath) ? libVmaPath : null;
    }

    /// <summary>
    /// Gets the value for LD_LIBRARY_PATH environment variable to use VMA libraries.
    /// </summary>
    /// <param name="preferSystem">
    /// If true, system libraries are preferred and VMA libs are appended.
    /// If false (default), VMA libs are prepended to have priority.
    /// </param>
    /// <returns>
    /// The new LD_LIBRARY_PATH value, or null if not on Linux or distro not detected.
    /// </returns>
    public static string? GetLdLibraryPath(bool preferSystem = false)
    {
        var libVmaPath = GetLibVmaPath();
        if (libVmaPath == null)
            return null;

        var vmaLibDir = Path.GetDirectoryName(libVmaPath);
        if (vmaLibDir == null)
            return null;

        var currentLdLibraryPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");

        if (preferSystem)
        {
            // Prefer system libraries: ${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}GetLibVmaPath()
            // Only add separator if LD_LIBRARY_PATH is set and not empty
            return string.IsNullOrWhiteSpace(currentLdLibraryPath)
                ? vmaLibDir
                : $"{currentLdLibraryPath}:{vmaLibDir}";
        }

        // Prefer VMA libraries: GetLibVmaPath():$LD_LIBRARY_PATH
        return string.IsNullOrWhiteSpace(currentLdLibraryPath)
            ? vmaLibDir
            : $"{vmaLibDir}:{currentLdLibraryPath}";
    }

    /// <summary>
    /// Gets the value for LD_PRELOAD environment variable to preload VMA library.
    /// </summary>
    /// <returns>
    /// The new LD_PRELOAD value with libvma.so added if not already present, or null if not on Linux or distro not detected.
    /// </returns>
    public static string? GetLdPreload()
    {
        var currentLdPreload = Environment.GetEnvironmentVariable("LD_PRELOAD");
        var currentIsEmpty = string.IsNullOrWhiteSpace(currentLdPreload);

        var libVmaPath = GetLibVmaPath();

        if (libVmaPath == null)
            return currentIsEmpty ? null : currentLdPreload;

        // Check if libvma is already in LD_PRELOAD
        if (!currentIsEmpty && currentLdPreload.Contains("libvma"))
            return currentLdPreload;

        // Add libvma.so to LD_PRELOAD
        return currentIsEmpty
            ? libVmaPath
            : $"{currentLdPreload}:{libVmaPath}";
    }

    // See https://docs.nvidia.com/networking/display/vmav9880/vma-configuration

    /// <summary>
    /// Gets the VMA_TRACELEVEL environment variable assignment string.
    /// </summary>
    /// <param name="level">The trace level to set.</param>
    /// <returns>String in format "VMA_TRACELEVEL=N"</returns>
    public static string GetVmaTraceLevelEnv(VmaTraceLevel level) => $"VMA_TRACELEVEL={((int)level)}";

    /// <summary>
    /// Gets the VMA_LOG_DETAILS environment variable assignment string.
    /// </summary>
    /// <param name="details">The log details level to set.</param>
    /// <returns>String in format "VMA_LOG_DETAILS=N"</returns>
    public static string GetVmaLogDetailsEnv(VmaLogDetails details) => $"VMA_LOG_DETAILS={((int)details)}";

    /// <summary>
    /// Gets the VMA_LOG_FILE environment variable assignment string.
    /// </summary>
    /// <remarks>
    /// Redirects all VMA logging to a specific user-defined file.
    /// This is very useful when raising the VMA_TRACELEVEL. The VMA replaces a single '%d' appearing in the log file name with the pid of the process loaded with VMA.
    /// This can help when running multiple instances of VMA, each with its own log file name.Example: VMA_LOG_FILE=/tmp/vma_log.txt
    /// </remarks>
    /// <param name="logFilePath">The log file path. Use %d to include process ID.</param>
    /// <returns>String in format "VMA_LOG_FILE=path"</returns>
    public static string GetVmaLogFileEnv(string logFilePath) => $"VMA_LOG_FILE={logFilePath}";

    /// <summary>
    /// Gets the VMA_STATS_FILE environment variable assignment string.
    /// </summary>
    /// <remarks>
    /// Redirects socket statistics to a specific user-defined file. VMA dumps each socket's statistics into a file when closing the socket. Example: VMA_STATS_FILE=/tmp/stats
    /// </remarks>
    /// <param name="statsFilePath">The stats file path.</param>
    /// <returns>String in format "VMA_STATS_FILE=path"</returns>
    public static string GetVmaStatsFileEnv(string statsFilePath) => $"VMA_STATS_FILE={statsFilePath}";

    /// <summary>
    /// Retrieves the VMA API pointer using getsockopt(SO_VMA_GET_API) and marshals it to <see cref="VmaApi"/>.
    /// </summary>
    /// <param name="socket">Socket used to issue the getsockopt call.</param>
    /// <param name="api">Populated VMA API struct when available.</param>
    /// <returns>True when VMA is present and the API pointer was resolved.</returns>
    internal static bool TryGetVmaApi(Socket socket, out VmaApi api)
    {
        api = default;

        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        if (!OperatingSystem.IsLinux())
            return false;

        try
        {
            Span<byte> opt = stackalloc byte[IntPtr.Size];

            const int SOL_SOCKET = 1;
            const int SO_VMA_GET_API = 2800;

            var result = socket.GetRawSocketOption(SOL_SOCKET, SO_VMA_GET_API, opt);
            if (result != 0)
                return false;

            long ptrValue = IntPtr.Size == 8 ? BitConverter.ToInt64(opt) : BitConverter.ToInt32(opt);

            if (ptrValue == 0)
                return false;

            var apiPtr = new IntPtr(ptrValue);
            api = Marshal.PtrToStructure<VmaApi>(apiPtr);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether the provided socket is offloaded by VMA.
    /// This probes VMA's get_socket_rings_num API: a non-negative value means the FD is offloaded.
    /// </summary>
    /// <param name="socket">Socket to test.</param>
    /// <returns>True if offloaded; false if VMA is absent or the socket is not offloaded.</returns>
    public static bool IsSocketOffloaded(Socket socket)
    {
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        if (!TryGetVmaApi(socket, out var api))
            return false;

        try
        {
            return api.GetSocketRingsNum(checked((int)socket.Handle)) >= 0;
        }
        catch
        {
            return false;
        }
    }
}
