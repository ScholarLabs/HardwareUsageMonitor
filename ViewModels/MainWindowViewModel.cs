using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace HardwareUsageMonitor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public SystemStatusViewModel System { get; }

    public DockerViewModel Docker { get; }

    public IAsyncRelayCommand RefreshAllCommand { get; }

    public DispatcherTimer Timer { get; }

    public MainWindowViewModel()
        : this(new SystemStatusViewModel(), new DockerViewModel())
    {
    }

    public MainWindowViewModel(SystemStatusViewModel system, DockerViewModel docker)
    {
        System = system;
        Docker = docker;

        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync);

        Timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5),
        };

        Timer.Tick += Timer_Tick;

        Timer.Start();

        _ = RefreshAllAsync();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        await RefreshAllAsync();
    }

    public async Task RefreshAllAsync()
    {
        await System.RefreshAsync();
        await Docker.RefreshAsync();
    }
}