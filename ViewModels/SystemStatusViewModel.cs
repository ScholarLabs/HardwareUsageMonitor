using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HardwareUsageMonitor.Models;
using HardwareUsageMonitor.Services.SystemMonitoring;

namespace HardwareUsageMonitor.ViewModels;

public class SystemStatusViewModel : ViewModelBase
{
    public ISystemMonitorService Service { get; }

    public string CpuText { get; private set; } = "CPU: -";

    public string RamText { get; private set; } = "RAM: -";

    public string LastUpdateText { get; private set; } = "Ostatnie odswiezenie: -";

    public SystemStats? LatestStats { get; private set; }

    public IReadOnlyList<CpuCoreStats> LatestCpuCores { get; private set; } = [];

    public SystemStatusViewModel()
        : this(new WindowsSystemMonitorService())
    {
    }

    public SystemStatusViewModel(ISystemMonitorService service)
    {
        Service = service;
    }

    public Task RefreshAsync()
    {
        try
        {
            var stats = Service.GetSystemStats();
            LatestStats = stats;
            LatestCpuCores = Service.GetCpuCores();

            CpuText = $"CPU: {stats.CpuUsagePercent:0.00}%";
            RamText = $"RAM: {FormatBytes(stats.UsedMemoryBytes)} / {FormatBytes(stats.TotalMemoryBytes)} ({stats.MemoryUsagePercent:0.00}%)";
            LastUpdateText = $"Ostatnie odswiezenie: {stats.Timestamp:HH:mm:ss}";
        }
        catch (Exception)
        {
            LatestStats = null;
            LatestCpuCores = [];
            CpuText = "CPU: -";
            RamText = "RAM: -";
        }

        OnPropertyChanged(nameof(CpuText));
        OnPropertyChanged(nameof(RamText));
        OnPropertyChanged(nameof(LastUpdateText));
        OnPropertyChanged(nameof(LatestStats));
        OnPropertyChanged(nameof(LatestCpuCores));

        return Task.CompletedTask;
    }

    private static string FormatBytes(ulong bytes)
    {
        const double kiloByte = 1024;
        var megaBytes = bytes / kiloByte / kiloByte;

        if (megaBytes < 1024)
        {
            return $"{megaBytes:0} MB"; // pokazujemy MB
        }

        return $"{megaBytes / kiloByte:0.00} GB"; // pokazujemy GB
    }
}
