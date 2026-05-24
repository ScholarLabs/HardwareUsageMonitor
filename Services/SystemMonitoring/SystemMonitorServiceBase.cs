using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.SystemMonitoring;

public abstract class SystemMonitorServiceBase : ISystemMonitorService
{
    public abstract SystemStats GetSystemStats();

    public virtual IReadOnlyList<ProcessInfo> GetProcesses()
    {
        return Process.GetProcesses()
            .Select(CreateProcessInfo)
            .OrderByDescending(process => process.WorkingSetBytes)
            .ToArray();
    }

    protected static T GetValueOrDefault<T>(Func<T> valueFactory, T defaultValue)
    {
        try
        {
            return valueFactory();
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or NotSupportedException
            or System.ComponentModel.Win32Exception)
        {
            return defaultValue;
        }
    }

    private static ProcessInfo CreateProcessInfo(Process process)
    {
        using (process)
        {
            return new ProcessInfo(
                process.Id,
                GetValueOrDefault(() => process.ProcessName, "Unknown"),
                GetValueOrDefault(() => process.WorkingSet64, 0),
                GetValueOrDefault(() => process.PrivateMemorySize64, 0),
                GetValueOrDefault(() => process.Threads.Count, 0),
                GetValueOrDefault<DateTime?>(() => process.StartTime, null));
        }
    }
}
