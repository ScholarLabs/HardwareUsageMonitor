using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.SystemMonitoring;

public sealed class LinuxSystemMonitorService : SystemMonitorServiceBase
{
    private readonly object _cpuLock = new();
    private CpuTimes? _previousTotal;
    private CpuTimes[]? _previousCores;

    public override SystemStats GetSystemStats()
    {
        var (totalBytes, availableBytes) = ReadMemInfo();
        var usedBytes = totalBytes - availableBytes;
        var memoryPercent = totalBytes == 0 ? 0d : usedBytes * 100d / totalBytes;
        var cpuPercent = GetCpuUsagePercent();

        return new SystemStats(
            Math.Round(cpuPercent, 2),
            totalBytes,
            availableBytes,
            usedBytes,
            Math.Round(memoryPercent, 2),
            DateTimeOffset.Now);
    }

    public override IReadOnlyList<CpuCoreStats> GetCpuCores()
    {
        lock (_cpuLock)
        {
            var current = ReadAllCpuTimes();
            var previous = _previousCores;

            if (previous is null || previous.Length != current.Length)
            {
                previous = current;
                Thread.Sleep(250);
                current = ReadAllCpuTimes();
            }

            _previousCores = current;

            var cores = new CpuCoreStats[current.Length];
            for (var i = 0; i < current.Length; i++)
            {
                var idleDelta = current[i].Idle - previous[i].Idle;
                var totalDelta = current[i].Total - previous[i].Total;
                var usage = totalDelta <= 0 ? 0d : (1d - idleDelta / (double)totalDelta) * 100d;
                cores[i] = new CpuCoreStats($"Core {i}", Math.Round(Math.Clamp(usage, 0, 100), 2));
            }

            return cores;
        }
    }

    private double GetCpuUsagePercent()
    {
        lock (_cpuLock)
        {
            var current = ReadTotalCpuTimes();
            CpuTimes previous;

            if (_previousTotal is null)
            {
                previous = current;
                Thread.Sleep(250);
                current = ReadTotalCpuTimes();
            }
            else
            {
                previous = _previousTotal.Value;
            }

            _previousTotal = current;

            var idleDelta = current.Idle - previous.Idle;
            var totalDelta = current.Total - previous.Total;

            if (totalDelta <= 0)
                return 0;

            return Math.Clamp((1d - idleDelta / (double)totalDelta) * 100d, 0, 100);
        }
    }

    private static CpuTimes ReadTotalCpuTimes()
    {
        var line = File.ReadLines("/proc/stat").GetEnumerator();
        line.MoveNext();
        return ParseCpuLine(line.Current);
    }

    private static CpuTimes[] ReadAllCpuTimes()
    {
        var cores = new List<CpuTimes>();
        foreach (var line in File.ReadLines("/proc/stat"))
        {
            if (line.StartsWith("cpu") && line.Length > 3 && char.IsDigit(line[3]))
                cores.Add(ParseCpuLine(line));
        }
        return cores.ToArray();
    }

    private static CpuTimes ParseCpuLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var user = parts.Length > 1 ? ulong.Parse(parts[1]) : 0;
        var nice = parts.Length > 2 ? ulong.Parse(parts[2]) : 0;
        var system = parts.Length > 3 ? ulong.Parse(parts[3]) : 0;
        var idle = parts.Length > 4 ? ulong.Parse(parts[4]) : 0;
        var iowait = parts.Length > 5 ? ulong.Parse(parts[5]) : 0;
        var irq = parts.Length > 6 ? ulong.Parse(parts[6]) : 0;
        var softirq = parts.Length > 7 ? ulong.Parse(parts[7]) : 0;
        var steal = parts.Length > 8 ? ulong.Parse(parts[8]) : 0;

        var idleTotal = idle + iowait;
        var total = user + nice + system + idle + iowait + irq + softirq + steal;
        return new CpuTimes(idleTotal, total);
    }

    private static (ulong Total, ulong Available) ReadMemInfo()
    {
        ulong total = 0, available = 0;
        foreach (var line in File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:"))
                total = ParseMemInfoKb(line) * 1024;
            else if (line.StartsWith("MemAvailable:"))
                available = ParseMemInfoKb(line) * 1024;

            if (total != 0 && available != 0)
                break;
        }
        return (total, available);
    }

    private static ulong ParseMemInfoKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && ulong.TryParse(parts[1], out var value) ? value : 0;
    }

    private readonly record struct CpuTimes(ulong Idle, ulong Total);
}
