using System;

namespace HardwareUsageMonitor.Models;

public sealed record ProcessInfo(
    int Id,
    string Name,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    int ThreadCount,
    DateTime? StartTime);
