using System;
using System.Collections.ObjectModel;
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

    public IAsyncRelayCommand StartCommand { get; }

    public IAsyncRelayCommand StopCommand { get; }

    public DockerViewModel()
        : this(new DockerCliService())
    {
    }

    public DockerViewModel(IDockerService service)
    {
        Service = service;
        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
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

            StatusText = $"Docker: {Containers.Count} kontenerow";
        }
        catch (Exception exception)
        {
            Containers.Clear();
            StatusText = $"Docker: blad {exception.Message}";
        }

        OnPropertyChanged(nameof(StatusText));
    }

    public async Task StartAsync()
    {
        if (SelectedContainer is null)
        {
            return;
        }

        try
        {
            await Service.StartContainerAsync(SelectedContainer);
            await RefreshAsync();
        }
        catch (Exception)
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public async Task StopAsync()
    {
        if (SelectedContainer is null)
        {
            return;
        }

        try
        {
            await Service.StopContainerAsync(SelectedContainer);
            await RefreshAsync();
        }
        catch (Exception)
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
