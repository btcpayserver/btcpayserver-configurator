using System.Collections.Concurrent;
using System.Text;
using BTCPayServerDockerConfigurator.Controllers;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace BTCPayServerDockerConfigurator;

public class DeploymentService
{
    private readonly IOptions<ConfiguratorOptions> _options;
    private readonly ILogger<DeploymentService> _logger;

    private readonly ConcurrentDictionary<string,
            (Task<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>, StringBuilder)>
        OngoingDeployments = new();

    public DeploymentService(IOptions<ConfiguratorOptions> options,
        ILogger<DeploymentService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public string StartDeployment(ConfiguratorSettings configuratorSettings, bool verified)
    {
        var id = Guid.NewGuid().ToString();
        var sb = new StringBuilder();

        var tcs =
            new TaskCompletionSource<
                UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>();
        _ = Task.Run(async () =>
        {
            var bash = configuratorSettings.ConstructBashFile();

            if (configuratorSettings.DeploymentSettings.DeploymentType == DeploymentType.Manual)
            {
                tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>
                {
                    Additional = new DeployAdditionalData { Bash = bash },
                    Json = configuratorSettings.ToString(),
                    Settings = configuratorSettings
                });
                return;
            }

            var ssh = configuratorSettings.GetSshSettings(_options.Value, verified);

            try
            {
                using var connection = await ssh.ConnectAsync();
                var shell =
                    connection.CreateShellStream("xterm", 50, 50, 640, 480, 17640);
                var commands = bash
                    .Replace(Environment.NewLine, "\n")
                    .Split("\n", StringSplitOptions.RemoveEmptyEntries);

                var result = await RunCommandsInShellSequential(commands, shell, sb);
                var exitcode = result.Item1 ? 0 : -1;

                tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>
                {
                    Additional = new DeployAdditionalData
                    {
                        Bash = bash,
                        Error = "",
                        ExitStatus = exitcode,
                        Output = result.Item2.Replace(Environment.NewLine, "\n"),
                        InProgress = false
                    },
                    Json = configuratorSettings.ToString(),
                    Settings = configuratorSettings
                });

                shell.Dispose();
            }
            catch (Exception e)
            {
                tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>
                {
                    Additional = new DeployAdditionalData
                    {
                        Bash = bash,
                        Error = e.Message,
                        ExitStatus = -1,
                        InProgress = false
                    },
                    Json = configuratorSettings.ToString(),
                    Settings = configuratorSettings
                });
            }
        });

        OngoingDeployments.TryAdd(id, (tcs.Task, sb));
        return id;
    }

    private async Task<(bool, string)> RunCommandsInShellSequential(string[] commands,
        ShellStream shellStream, StringBuilder result)
    {
        var failed = false;
        var finished = false;
        var cts = new TaskCompletionSource<bool>();
        shellStream.ErrorOccurred += (sender, args) =>
        {
            result.AppendLine(args.Exception.Message);
            failed = true;
            cts.TrySetResult(false);
        };
        shellStream.DataReceived += (sender, args) =>
        {
            var str = Encoding.UTF8.GetString(args.Data);
            if (!str.Contains("end-of-command"))
            {
                result.Append(str);
            }

            if (str.Contains("end-of-command 2"))
            {
                finished = true;
            }
        };
        shellStream.Read();
        for (var index = 0; index < commands.Length; index++)
        {
            var command = commands[index];
            if (failed)
                break;

            shellStream.WriteLine(command);
            waiter:
            shellStream.WriteLine("echo \"end-of-command $(expr 1 + 1)\"");
            var counter = 0;
            while (!finished)
            {
                if (counter > 10)
                {
                    goto waiter;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
                counter++;
            }

            if (commands.Length == index + 1)
            {
                cts.TrySetResult(true);
            }

            finished = false;
        }

        failed = !await cts.Task;
        return (!failed, result.ToString());
    }

    public UpdateSettings<ConfiguratorSettings, DeployAdditionalData> GetDeploymentResult(
        string id)
    {
        if (!OngoingDeployments.ContainsKey(id))
            return null;

        if (OngoingDeployments[id].Item1.IsCompleted)
        {
            return OngoingDeployments.Remove(id, out var result) ? result.Item1.Result : null;
        }

        return new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>
        {
            Additional = new DeployAdditionalData
            {
                InProgress = true,
                Output = OngoingDeployments[id].Item2.ToString()
                    .Replace(Environment.NewLine, "\n")
            }
        };
    }
}
