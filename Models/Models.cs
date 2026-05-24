namespace HardwareUsageMonitor.Models;

public class CpuCore
{
    public string Name { get; set; } = string.Empty;

    public double Usage { get; set; }
}

public sealed record CpuCoreStats(string Name, double Usage);
