using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HardwareUsageMonitor.Models;

namespace HardwareUsageMonitor.Services.Docker;

public sealed class DockerCliService : IDockerService
{
    public async Task<IReadOnlyList<DockerContainerInfo>> GetContainersAsync()
    {
        var result = await RunDockerAsync(
            "container",
            "ls",
            "-a",
            "--format",
            "{{.ID}}\t{{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Label \"com.docker.compose.project\"}}");

        var containers = new List<DockerContainerInfo>();

        if (result.ExitCode != 0)
        {
            return containers;
        }

        foreach (var line in result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');

            if (parts.Length < 4)
            {
                continue;
            }

            var projectName = parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4])
                ? parts[4]
                : "Pozostałe";

            containers.Add(new DockerContainerInfo(
                parts[0],
                parts[1],
                parts[2],
                parts[3],
                projectName,
                parts[3].StartsWith("Up", StringComparison.OrdinalIgnoreCase)));
        }

        return containers;
    }

    public Task StartContainerAsync(DockerContainerInfo container)
    {
        return RunDockerCommandAsync("start", container.Id);
    }

    public Task StopContainerAsync(DockerContainerInfo container)
    {
        return RunDockerCommandAsync("stop", container.Id);
    }

    private static async Task RunDockerCommandAsync(params string[] arguments)
    {
        var result = await RunDockerAsync(arguments);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Error.Trim());
        }
    }

    private static async Task<CommandResult> RunDockerAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Nie udało się uruchomić polecenia docker.");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new CommandResult(process.ExitCode, output, error);
    }

    private sealed record CommandResult(int ExitCode, string Output, string Error);
}
