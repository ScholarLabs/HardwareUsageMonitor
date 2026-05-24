using System;
using System.Collections.Generic;
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

    public ObservableCollection<DockerContainerInfo> Containers { get; } = new();

    public ObservableCollection<DockerProjectInfo> Projects { get; } = new();

    public IAsyncRelayCommand<DockerContainerInfo> ToggleContainerCommand { get; }

    public DockerViewModel()
        : this(new DockerCliService())
    {
    }

    public DockerViewModel(IDockerService service)
    {
        Service = service;
        ToggleContainerCommand = new AsyncRelayCommand<DockerContainerInfo>(ToggleAsync);
    }

    public async Task RefreshAsync()
    {
        try
        {
            var containers = await Service.GetContainersAsync();

            UpdateContainers(containers);
            UpdateProjects(containers);
            SetStatus($"Docker: {Containers.Count} kontenerów");
        }
        catch (Exception exception)
        {
            Containers.Clear();
            Projects.Clear();
            SetStatus($"Docker: błąd - {exception.Message}");
        }
    }

    private async Task ToggleAsync(DockerContainerInfo? container)
    {
        if (container is null)
        {
            return;
        }

        try
        {
            if (container.IsRunning)
            {
                await Service.StopContainerAsync(container);
            }
            else
            {
                await Service.StartContainerAsync(container);
            }

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            SetStatus($"Docker: błąd - {exception.Message}");
        }
    }

    private void UpdateContainers(IEnumerable<DockerContainerInfo> containers)
    {
        Containers.Clear();

        foreach (var container in containers)
        {
            Containers.Add(container);
        }
    }

    private void UpdateProjects(IEnumerable<DockerContainerInfo> containers)
    {
        Projects.Clear();

        foreach (var group in containers.GroupBy(container => container.ProjectName).OrderBy(group => group.Key))
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

    private void SetStatus(string statusText)
    {
        StatusText = statusText;
        OnPropertyChanged(nameof(StatusText));
    }
}
