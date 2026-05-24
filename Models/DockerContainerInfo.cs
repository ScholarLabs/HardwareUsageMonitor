namespace HardwareUsageMonitor.Models;

public sealed record DockerContainerInfo(
    string Id,
    string Name,
    string Image,
    string Status,
    bool IsRunning)
{
    public string DisplayText => $"{Name} | {Image} | {Status}";
}
