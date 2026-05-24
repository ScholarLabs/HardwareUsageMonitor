using System;

namespace HardwareUsageMonitor.Models;

public sealed record SystemStats(
    double CpuUsagePercent,
    ulong TotalMemoryBytes,
    ulong AvailableMemoryBytes,
    ulong UsedMemoryBytes,
    double MemoryUsagePercent,
    DateTimeOffset Timestamp);
