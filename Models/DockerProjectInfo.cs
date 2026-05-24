using System.Collections.ObjectModel;

namespace HardwareUsageMonitor.Models;

public sealed class DockerProjectInfo
{
    public string ProjectName { get; init; } = string.Empty;

    public ObservableCollection<DockerContainerInfo> Containers { get; } = new();
}
