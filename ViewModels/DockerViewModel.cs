using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using HardwareUsageMonitor.Models;
using HardwareUsageMonitor.Services.Docker;

namespace HardwareUsageMonitor.ViewModels;

public class DockerViewModel : ViewModelBase
{
    public IDockerService Service { get; }

    public string StatusText { get; private set; } = "Docker: -";

    public DockerContainerInfo? SelectedContainer { get; set; }

    public ObservableCollection<DockerContainerInfo> Containers { get; } = new();

    public ObservableCollection<DockerProjectInfo> Projects { get; } = new();

    public IAsyncRelayCommand StartCommand { get; }

    public IAsyncRelayCommand StopCommand { get; }

    public IAsyncRelayCommand<DockerContainerInfo> StartContainerCommand { get; }

    public IAsyncRelayCommand<DockerContainerInfo> StopContainerCommand { get; }

    public IAsyncRelayCommand<DockerContainerInfo> ToggleContainerCommand { get; }

    public DockerViewModel()
        : this(new DockerCliService())
    {
    }

    public DockerViewModel(IDockerService service)
    {
        Service = service;
        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        StartContainerCommand = new AsyncRelayCommand<DockerContainerInfo>(StartAsync);
        StopContainerCommand = new AsyncRelayCommand<DockerContainerInfo>(StopAsync);
        ToggleContainerCommand = new AsyncRelayCommand<DockerContainerInfo>(ToggleAsync);
    }

    public async Task RefreshAsync()
    {
        try
        {
            StatusText = "Docker: -";
            OnPropertyChanged(nameof(StatusText));

            var containers = await Service.GetContainersAsync();

            Containers.Clear();
            foreach (var container in containers)
            {
                Containers.Add(container);
            }

            RefreshProjects();
            StatusText = $"Docker: {Containers.Count} kontenerów";
        }
        catch (Exception exception)
        {
            Containers.Clear();
            Projects.Clear();
            StatusText = $"Docker: blad {exception.Message}";
        }

        OnPropertyChanged(nameof(StatusText));
    }

    public async Task StartAsync()
    {
        await StartAsync(SelectedContainer);
    }

    public async Task StopAsync()
    {
        await StopAsync(SelectedContainer);
    }

    private async Task StartAsync(DockerContainerInfo? container)
    {
        if (container is null)
        {
            return;
        }

        try
        {
            await Service.StartContainerAsync(container);
            await RefreshAsync();
        }
        catch (Exception)
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private async Task StopAsync(DockerContainerInfo? container)
    {
        if (container is null)
        {
            return;
        }

        try
        {
            await Service.StopContainerAsync(container);
            await RefreshAsync();
        }
        catch (Exception)
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private async Task ToggleAsync(DockerContainerInfo? container)
    {
        if (container is null)
        {
            return;
        }

        if (container.IsRunning)
        {
            await StopAsync(container);
            return;
        }

        await StartAsync(container);
    }

    private void RefreshProjects()
    {
        Projects.Clear();

        foreach (var group in Containers.GroupBy(container => container.ProjectName).OrderBy(group => group.Key))
        {
            var project = new DockerProjectInfo
            {
                ProjectName = group.Key,
            };

            foreach (var container in group.OrderBy(container => container.Name))
            {
                project.Containers.Add(container);
            }

            Projects.Add(project);
        }
    }
}
