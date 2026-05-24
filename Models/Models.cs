using System.Collections.ObjectModel;

namespace HardwareUsageMonitor.Models
{
    public class CpuCore
    {
        public string Name { get; set; }
        public double Usage { get; set; }
    }

    public class DockerContainer
    {
        public string Name { get; set; }
        public double CpuUsage { get; set; }
        public double RamUsage { get; set; }
        public bool IsRunning { get; set; }
    }
    public class DockerProject
    {
        public string ProjectName { get; set; }
        // Każdy projekt ma swoją własną listę kontenerów
        public ObservableCollection<DockerContainer> Containers { get; set; }
    }
}