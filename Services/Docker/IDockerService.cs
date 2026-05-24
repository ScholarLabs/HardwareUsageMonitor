using System.Collections.Generic;
using System.Threading.Tasks;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.Docker;

public interface IDockerService
{
    Task<IReadOnlyList<DockerContainerInfo>> GetContainersAsync();

    Task StartContainerAsync(DockerContainerInfo container);

    Task StopContainerAsync(DockerContainerInfo container);
}
