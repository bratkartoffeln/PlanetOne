using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PlanetOne.Platform
{
    public static class ProcessMemory
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Counters
        {
            public uint Size,PageFaultCount;
            public UIntPtr PeakWorkingSet,WorkingSet,PeakPagedPool,PagedPool,PeakNonPagedPool,NonPagedPool,Pagefile,PeakPagefile;
        }
        [DllImport("kernel32.dll")]private static extern IntPtr GetCurrentProcess();
        [DllImport("psapi.dll",SetLastError=true)]private static extern bool GetProcessMemoryInfo(IntPtr process,out Counters counters,uint size);
        public static long WorkingSetBytes()
        {
            Counters counters;
            if(!GetProcessMemoryInfo(GetCurrentProcess(),out counters,(uint)Marshal.SizeOf(typeof(Counters))))throw new Win32Exception(Marshal.GetLastWin32Error());
            return checked((long)counters.WorkingSet.ToUInt64());
        }
    }
}
