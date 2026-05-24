using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const int RefreshIntervalSeconds = 1;

    private bool _isRefreshing;
    private int _refreshCountdown = RefreshIntervalSeconds;
    private double _refreshIconAngle;

    public SystemStatusViewModel System { get; }

    public DockerViewModel Docker { get; }

    public IAsyncRelayCommand RefreshAllCommand { get; }

    public DispatcherTimer Timer { get; }

    public DispatcherTimer SpinTimer { get; }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    public int RefreshCountdown
    {
        get => _refreshCountdown;
        private set
        {
            if (SetProperty(ref _refreshCountdown, value))
            {
                OnPropertyChanged(nameof(RefreshCountdownText));
            }
        }
    }

    public string RefreshCountdownText => $"Odświeżanie za {RefreshCountdown}";

    public double RefreshIconAngle
    {
        get => _refreshIconAngle;
        private set => SetProperty(ref _refreshIconAngle, value);
    }

    public double CpuUsage { get; private set; }

    public string CpuDisplay { get; private set; } = "-";

    public double RamUsage { get; private set; }

    public string RamDisplay { get; private set; } = "-";

    public string LastUpdateDisplay { get; private set; } = "Ostatnie odswiezenie: -";

    public ObservableCollection<CpuCore> CpuCores { get; } = new();

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
            Interval = TimeSpan.FromSeconds(1),
        };

        Timer.Tick += Timer_Tick;
        Timer.Start();

        SpinTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };

        SpinTimer.Tick += SpinTimer_Tick;

        _ = RefreshAllAsync();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (IsRefreshing)
        {
            return;
        }

        RefreshCountdown--;

        if (RefreshCountdown <= 0)
        {
            await RefreshAllAsync();
        }
    }

    private void SpinTimer_Tick(object? sender, EventArgs e)
    {
        RefreshIconAngle = (RefreshIconAngle + 24) % 360;
    }

    public async Task RefreshAllAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            SpinTimer.Start();

            await System.RefreshAsync();
            UpdateSystemDisplay(System.LatestStats);

            await Docker.RefreshAsync();
        }
        finally
        {
            SpinTimer.Stop();
            RefreshIconAngle = 0;
            RefreshCountdown = RefreshIntervalSeconds;
            IsRefreshing = false;
        }
    }

    private void UpdateSystemDisplay(SystemStats? stats)
    {
        if (stats is null)
        {
            CpuUsage = 0;
            CpuDisplay = "-";
            RamUsage = 0;
            RamDisplay = "-";
            LastUpdateDisplay = "Ostatnie odswiezenie: -";
            CpuCores.Clear();
        }
        else
        {
            CpuUsage = stats.CpuUsagePercent;
            CpuDisplay = $"{stats.CpuUsagePercent:0.0}%";
            RamUsage = stats.MemoryUsagePercent;
            RamDisplay = $"{FormatBytes(stats.UsedMemoryBytes)} / {FormatBytes(stats.TotalMemoryBytes)}";
            LastUpdateDisplay = $"Ostatnie odswiezenie: {stats.Timestamp:HH:mm:ss}";

            UpdateCpuCores(System.LatestCpuCores);
        }

        OnPropertyChanged(nameof(CpuUsage));
        OnPropertyChanged(nameof(CpuDisplay));
        OnPropertyChanged(nameof(RamUsage));
        OnPropertyChanged(nameof(RamDisplay));
        OnPropertyChanged(nameof(LastUpdateDisplay));
    }

    private void UpdateCpuCores(IReadOnlyCollection<CpuCoreStats> cpuCores)
    {
        CpuCores.Clear();

        if (cpuCores.Count == 0)
        {
            CpuCores.Add(new CpuCore { Name = "CPU", Usage = CpuUsage });
            return;
        }

        foreach (var core in cpuCores)
        {
            CpuCores.Add(new CpuCore { Name = core.Name, Usage = core.Usage });
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double kiloByte = 1024;
        var megaBytes = bytes / kiloByte / kiloByte;

        if (megaBytes < 1024)
        {
            return $"{megaBytes:0} MB";
        }

        return $"{megaBytes / kiloByte:0.00} GB";
    }
}
