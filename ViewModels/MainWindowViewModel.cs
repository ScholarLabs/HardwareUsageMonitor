using System.Collections.ObjectModel;

namespace HardwareUsageMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public double RamUsage { get; set; } = 42.5;
        public string RamDisplay { get; set; } = "6.8 / 16 GB";


        public ObservableCollection<CpuCore> CpuCores { get; set; }
        public ObservableCollection<DockerProject> Projects { get; set; }

        public MainWindowViewModel()
        {

            CpuCores = new ObservableCollection<CpuCore>
            {
                new CpuCore { Name = "Core 0", Usage = 18.5 },
                new CpuCore { Name = "Core 1", Usage = 74.0 },
                new CpuCore { Name = "Core 2", Usage = 92.1 },
                new CpuCore { Name = "Core 3", Usage = 34.5 }
            };


            Projects = new ObservableCollection<DockerProject>
            {
                new DockerProject
                {
                    ProjectName = "proxy-stack",
                    Containers = new ObservableCollection<DockerContainer>
                    {
                        new DockerContainer { Name = "nginx-proxy", CpuUsage = 3.2, RamUsage = 112, IsRunning = true },
                        new DockerContainer { Name = "letsencrypt", CpuUsage = 0.0, RamUsage = 45, IsRunning = true }
                    }
                },
                new DockerProject
                {
                    ProjectName = "smarthome",
                    Containers = new ObservableCollection<DockerContainer>
                    {
                        new DockerContainer { Name = "home-assistant", CpuUsage = 14.2, RamUsage = 480, IsRunning = true },
                        new DockerContainer { Name = "mqtt-broker", CpuUsage = 1.1, RamUsage = 28, IsRunning = true },
                        new DockerContainer { Name = "zigbee2mqtt", CpuUsage = 5.0, RamUsage = 72, IsRunning = false }
                    }
                }
            };
        }
    }

    public class CpuCore
    {
        public string Name { get; set; } = string.Empty;
        public double Usage { get; set; }
    }

    public class DockerContainer
    {
        public string Name { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double RamUsage { get; set; }
        public bool IsRunning { get; set; }
    }

    public class DockerProject
    {
        public string ProjectName { get; set; } = string.Empty;
        public ObservableCollection<DockerContainer> Containers { get; set; } = new();
    }
}