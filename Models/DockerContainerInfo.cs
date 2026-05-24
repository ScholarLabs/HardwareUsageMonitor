namespace HardwareUsageMonitor.Models;

public sealed record DockerContainerInfo(
    string Id,
    string Name,
    string Image,
    string Status,
    string ProjectName,
    bool IsRunning)
{
    public string DisplayText => $"{Name} | {Image} | {Status}";
}
