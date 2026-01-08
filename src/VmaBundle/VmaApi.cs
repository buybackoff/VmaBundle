using System;
using System.Runtime.InteropServices;

namespace VmaBundle;

// AI generated, use with caution

internal static class LibC
{
    [DllImport("libc", EntryPoint="__errno_location")]
    public static extern IntPtr ErrnoLocation();

    public static int GetErrno() => Marshal.ReadInt32(ErrnoLocation());
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal readonly struct VmaApi
{
    [FieldOffset(0)] private readonly IntPtr _registerRecvCallback;
    [FieldOffset(8)] private readonly IntPtr _recvfromZcopy;
    [FieldOffset(16)] private readonly IntPtr _freePackets;
    [FieldOffset(24)] private readonly IntPtr _addConfRule;
    [FieldOffset(32)] private readonly IntPtr _threadOffload;
    [FieldOffset(40)] private readonly IntPtr _socketXtremePoll;
    [FieldOffset(48)] private readonly IntPtr _getSocketRingsNum;
    [FieldOffset(56)] private readonly IntPtr _getSocketRingsFds;
    [FieldOffset(64)] private readonly IntPtr _getSocketTxRingFd;
    [FieldOffset(72)] private readonly IntPtr _socketXtremeFreeVmaPackets;
    [FieldOffset(80)] private readonly IntPtr _socketXtremeRefVmaBuff;
    [FieldOffset(88)] private readonly IntPtr _socketXtremeFreeVmaBuff;
    [FieldOffset(96)] private readonly IntPtr _dumpFdStats;
    [FieldOffset(104)] private readonly IntPtr _vmaAddRingProfile;
    [FieldOffset(112)] private readonly IntPtr _getSocketNetworkHeader;
    [FieldOffset(120)] private readonly IntPtr _getRingDirectDescriptors;
    [FieldOffset(128)] private readonly IntPtr _registerMemoryOnRing;
    [FieldOffset(136)] private readonly IntPtr _deregisterMemoryOnRing;
    [FieldOffset(144)] private readonly IntPtr _vmaModifyRing;
    [FieldOffset(152)] private readonly ulong _vmaExtraSupportedMask;
    [FieldOffset(160)] private readonly IntPtr _ioctl;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int VmaRecvCallback(int fd, UIntPtr szIov, IntPtr iov, IntPtr vmaInfo, IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int RegisterRecvCallbackDelegate(int s, VmaRecvCallback callback, IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int RecvfromZcopyDelegate(int s, IntPtr buf, UIntPtr len, IntPtr flags, IntPtr from, IntPtr fromlen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int FreePacketsDelegate(int s, IntPtr pkts, UIntPtr count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int AddConfRuleDelegate(IntPtr configLine);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int ThreadOffloadDelegate(int offload, IntPtr tid);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SocketXtremePollDelegate(int fd, IntPtr completions, uint ncompletions, int flags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int GetSocketRingsNumDelegate(int fd);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int GetSocketRingsFdsDelegate(int fd, IntPtr ringFds, int ringFdsSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int GetSocketTxRingFdDelegate(int sockFd, IntPtr to, uint tolen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int SocketXtremeFreeVmaPacketsDelegate(IntPtr packets, int num);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int SocketXtremeRefVmaBuffDelegate(IntPtr buff);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int SocketXtremeFreeVmaBuffDelegate(IntPtr buff);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int DumpFdStatsDelegate(int fd, int logLevel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int VmaAddRingProfileDelegate(IntPtr profile, IntPtr key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int GetSocketNetworkHeaderDelegate(int fd, IntPtr ptr, IntPtr len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int GetRingDirectDescriptorsDelegate(int fd, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int RegisterMemoryOnRingDelegate(int fd, IntPtr addr, UIntPtr length, IntPtr key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int DeregisterMemoryOnRingDelegate(int fd, IntPtr addr, UIntPtr length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int VmaModifyRingDelegate(IntPtr mrData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate int IoctlDelegate(IntPtr cmsgHdr, UIntPtr cmsgLen);

    private static T GetDelegate<T>(IntPtr ptr) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(ptr);

    public int RegisterRecvCallback(int socketFd, VmaRecvCallback callback, IntPtr context)
    {
        if (_registerRecvCallback == IntPtr.Zero)
            return -1;
        var del = GetDelegate<RegisterRecvCallbackDelegate>(_registerRecvCallback);
        return del(socketFd, callback, context);
    }

    // public int RecvfromZcopy(int socketFd, IntPtr buffer, UIntPtr length, IntPtr flags, IntPtr from, IntPtr fromLength)
    // {
    //     if (_recvfromZcopy == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<RecvfromZcopyDelegate>(_recvfromZcopy);
    //     return del(socketFd, buffer, length, flags, from, fromLength);
    // }
    //
    // public int FreePackets(int socketFd, IntPtr packets, UIntPtr count)
    // {
    //     if (_freePackets == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<FreePacketsDelegate>(_freePackets);
    //     return del(socketFd, packets, count);
    // }

    // public int AddConfRule(string configLine)
    // {
    //     if (_addConfRule == IntPtr.Zero || string.IsNullOrEmpty(configLine))
    //         return -1;
    //     var del = GetDelegate<AddConfRuleDelegate>(_addConfRule);
    //     var ptr = Marshal.StringToHGlobalAnsi(configLine);
    //     try
    //     {
    //         return del(ptr);
    //     }
    //     finally
    //     {
    //         Marshal.FreeHGlobal(ptr);
    //     }
    // }

    // public int ThreadOffload(int offload, IntPtr tid)
    // {
    //     if (_threadOffload == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<ThreadOffloadDelegate>(_threadOffload);
    //     return del(offload, tid);
    // }

    // public int SocketXtremePoll(int fd, IntPtr completions, uint ncompletions, int flags)
    // {
    //     if (_socketXtremePoll == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<SocketXtremePollDelegate>(_socketXtremePoll);
    //     return del(fd, completions, ncompletions, flags);
    // }

    /// <summary>
    /// Returns the amount of rings that are associated with socket.
    /// 0 - Not a VMA-offloaded fd, -1 - failed call.
    /// </summary>
    /// <param name="fd">File Descriptor number of the socket.</param>
    /// <returns>On success, return the amount of rings. On error, -1 is returned.</returns>
    public int GetSocketRingsNum(int fd)
    {
        if (_getSocketRingsNum == IntPtr.Zero || (_vmaExtraSupportedMask & (1ul << 10)) == 0)
            return -1;
        
        var del = GetDelegate<GetSocketRingsNumDelegate>(_getSocketRingsNum);

        var result = del(fd);
        
        if (result != -1)
            return result;

        int errno = LibC.GetErrno();
        const int EINVAL = 22;
        if (errno == EINVAL)
        {
            // Not a VMA-offloaded fd: treat as "0 rings"
            return 0;
        }

        // other errors => surface as exception / special value
        
        return result;
    }

    // public int GetSocketRingsFds(int fd, IntPtr ringFds, int ringFdsSize)
    // {
    //     if (_getSocketRingsFds == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<GetSocketRingsFdsDelegate>(_getSocketRingsFds);
    //     return del(fd, ringFds, ringFdsSize);
    // }

    // public int GetSocketTxRingFd(int sockFd, IntPtr to, uint toLength)
    // {
    //     if (_getSocketTxRingFd == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<GetSocketTxRingFdDelegate>(_getSocketTxRingFd);
    //     return del(sockFd, to, toLength);
    // }

    // public int SocketXtremeFreeVmaPackets(IntPtr packets, int num)
    // {
    //     if (_socketXtremeFreeVmaPackets == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<SocketXtremeFreeVmaPacketsDelegate>(_socketXtremeFreeVmaPackets);
    //     return del(packets, num);
    // }

    // public int SocketXtremeRefVmaBuff(IntPtr buff)
    // {
    //     if (_socketXtremeRefVmaBuff == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<SocketXtremeRefVmaBuffDelegate>(_socketXtremeRefVmaBuff);
    //     return del(buff);
    // }
    //
    // public int SocketXtremeFreeVmaBuff(IntPtr buff)
    // {
    //     if (_socketXtremeFreeVmaBuff == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<SocketXtremeFreeVmaBuffDelegate>(_socketXtremeFreeVmaBuff);
    //     return del(buff);
    // }

    public int DumpFdStats(int fd, int logLevel)
    {
        if (_dumpFdStats == IntPtr.Zero)
            return -1;
        var del = GetDelegate<DumpFdStatsDelegate>(_dumpFdStats);
        return del(fd, logLevel);
    }

    // public int VmaAddRingProfile(IntPtr profile, IntPtr key)
    // {
    //     if (_vmaAddRingProfile == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<VmaAddRingProfileDelegate>(_vmaAddRingProfile);
    //     return del(profile, key);
    // }

    // public int GetSocketNetworkHeader(int fd, IntPtr buffer, IntPtr length)
    // {
    //     if (_getSocketNetworkHeader == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<GetSocketNetworkHeaderDelegate>(_getSocketNetworkHeader);
    //     return del(fd, buffer, length);
    // }
    //
    // public int GetRingDirectDescriptors(int fd, IntPtr data)
    // {
    //     if (_getRingDirectDescriptors == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<GetRingDirectDescriptorsDelegate>(_getRingDirectDescriptors);
    //     return del(fd, data);
    // }

    // public int RegisterMemoryOnRing(int fd, IntPtr addr, UIntPtr length, IntPtr key)
    // {
    //     if (_registerMemoryOnRing == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<RegisterMemoryOnRingDelegate>(_registerMemoryOnRing);
    //     return del(fd, addr, length, key);
    // }
    //
    // public int DeregisterMemoryOnRing(int fd, IntPtr addr, UIntPtr length)
    // {
    //     if (_deregisterMemoryOnRing == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<DeregisterMemoryOnRingDelegate>(_deregisterMemoryOnRing);
    //     return del(fd, addr, length);
    // }
    //
    // public int VmaModifyRing(IntPtr modifyRingData)
    // {
    //     if (_vmaModifyRing == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<VmaModifyRingDelegate>(_vmaModifyRing);
    //     return del(modifyRingData);
    // }
    // public int Ioctl(IntPtr cmsgHdr, UIntPtr cmsgLength)
    // {
    //     if (_ioctl == IntPtr.Zero)
    //         return -1;
    //     var del = GetDelegate<IoctlDelegate>(_ioctl);
    //     return del(cmsgHdr, cmsgLength);
    // }
}
