using System.Collections.Generic;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.SystemMonitoring;

public interface ISystemMonitorService
{
    SystemStats GetSystemStats();

    IReadOnlyList<ProcessInfo> GetProcesses();
}
