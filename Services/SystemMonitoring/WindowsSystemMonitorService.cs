using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.SystemMonitoring;

public sealed class WindowsSystemMonitorService : SystemMonitorServiceBase
{
    private readonly object _cpuLock = new();
    private CpuTimes? _previousCpuTimes;
    private CpuTimes[]? _previousCoreTimes;

    public override SystemStats GetSystemStats()
    {
        var memory = GetMemoryStatus();
        var cpuUsagePercent = GetCpuUsagePercent();
        var usedMemory = memory.TotalPhys - memory.AvailPhys;
        var memoryUsagePercent = memory.TotalPhys == 0
            ? 0
            : usedMemory * 100d / memory.TotalPhys;

        return new SystemStats(
            Math.Round(cpuUsagePercent, 2),
            memory.TotalPhys,
            memory.AvailPhys,
            usedMemory,
            Math.Round(memoryUsagePercent, 2),
            DateTimeOffset.Now);
    }

    private double GetCpuUsagePercent()
    {
        lock (_cpuLock)
        {
            var current = GetCpuTimes();
            CpuTimes previous;

            if (_previousCpuTimes is null)
            {
                previous = current;
                Thread.Sleep(250);
                current = GetCpuTimes();
            }
            else
            {
                previous = _previousCpuTimes.Value;
            }

            _previousCpuTimes = current;

            var idleDelta = current.Idle - previous.Idle;
            var totalDelta = current.Total - previous.Total;

            if (totalDelta <= 0)
            {
                return 0;
            }

            var usage = (1d - idleDelta / (double)totalDelta) * 100d;
            return Math.Clamp(usage, 0, 100);
        }
    }

    public override IReadOnlyList<CpuCoreStats> GetCpuCores()
    {
        lock (_cpuLock)
        {
            var current = GetProcessorTimes();
            var previous = _previousCoreTimes;

            if (previous is null || previous.Length != current.Length)
            {
                previous = current;
                Thread.Sleep(250);
                current = GetProcessorTimes();
            }

            _previousCoreTimes = current;

            var cores = new CpuCoreStats[current.Length];

            for (var index = 0; index < current.Length; index++)
            {
                var idleDelta = current[index].Idle - previous[index].Idle;
                var totalDelta = current[index].Total - previous[index].Total;
                var usage = totalDelta <= 0
                    ? 0
                    : (1d - idleDelta / (double)totalDelta) * 100d;

                cores[index] = new CpuCoreStats($"Core {index}", Math.Round(Math.Clamp(usage, 0, 100), 2));
            }

            return cores;
        }
    }

    private static CpuTimes GetCpuTimes()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            throw new InvalidOperationException("Unable to read system CPU times.");
        }

        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);

        return new CpuTimes(idle, kernel + user);
    }

    private static CpuTimes[] GetProcessorTimes()
    {
        var processorCount = Math.Max(Environment.ProcessorCount, 1);
        var itemSize = Marshal.SizeOf<ProcessorPerformanceInformation>();
        var bufferSize = itemSize * processorCount;
        var buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            var status = NtQuerySystemInformation(
                SystemProcessorPerformanceInformation,
                buffer,
                (uint)bufferSize,
                out var returnedLength);

            if (status != 0)
            {
                throw new InvalidOperationException($"Unable to read per-core CPU times. NT status: 0x{status:X8}.");
            }

            var actualCount = returnedLength == 0
                ? processorCount
                : Math.Min((int)returnedLength / itemSize, processorCount);
            var result = new CpuTimes[actualCount];

            for (var index = 0; index < actualCount; index++)
            {
                var pointer = IntPtr.Add(buffer, index * itemSize);
                var info = Marshal.PtrToStructure<ProcessorPerformanceInformation>(pointer);
                result[index] = new CpuTimes((ulong)info.IdleTime, (ulong)(info.KernelTime + info.UserTime));
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static MemoryStatusEx GetMemoryStatus()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>(),
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new InvalidOperationException("Unable to read system memory status.");
        }

        return status;
    }

    private static ulong ToUInt64(FileTime fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out FileTime idleTime,
        out FileTime kernelTime,
        out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        uint systemInformationLength,
        out uint returnLength);

    private const int SystemProcessorPerformanceInformation = 8;

    private readonly record struct CpuTimes(ulong Idle, ulong Total);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorPerformanceInformation
    {
        public long IdleTime;
        public long KernelTime;
        public long UserTime;
        public long DpcTime;
        public long InterruptTime;
        public uint InterruptCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
