using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Controllers;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.Options;
using Options = BTCPayServerDockerConfigurator.Models.Options;

namespace BTCPayServerDockerConfigurator
{
    public class DeploymentService
    {
        private readonly IOptions<Options> _options;

        ConcurrentDictionary<string, (Task<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>, StringBuilder)>
            OngoingDeployments =
                new ConcurrentDictionary<string, (Task<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>,
                    StringBuilder)>();

        public DeploymentService(IOptions<Options> options)
        {
            _options = options;
        }

        public async Task<string> StartDeployment(ConfiguratorSettings configuratorSettings)
        {
            var id = Guid.NewGuid().ToString();
            var sb = new StringBuilder();

            var tcs = new TaskCompletionSource<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>();
            _ = Task.Run(async () =>
            {
                var bash = configuratorSettings.ConstructBashFile();


                if (configuratorSettings.DeploymentSettings.DeploymentType == DeploymentType.Manual)
                {
                    tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                    {
                        Additional = new DeployAdditionalData()
                        {
                            Bash = bash
                        },
                        Json = configuratorSettings.ToString(),
                        Settings = configuratorSettings
                    });
                }

                var ssh = configuratorSettings.GetSshSettings(_options.Value);

                try
                {
                    var doOneLiner = false;


                    var connection = await ssh.ConnectAsync();

                    if (doOneLiner)
                    {
                        var oneliner = bash
                            .Replace(Environment.NewLine, "\n")
                            .Replace("\n", " && \n")
                            .TrimEnd(" && \n", StringComparison.InvariantCultureIgnoreCase);
                        var result = await connection.RunBash(
                            oneliner.Replace("\n", ""));
                        tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                        {
                            Additional = new DeployAdditionalData()
                            {
                                Bash = bash,
                                Error = result.Error,
                                Output = result.Output,
                                ExitStatus = result.ExitStatus
                            },
                            Json = configuratorSettings.ToString(),
                            Settings = configuratorSettings
                        });
                    }
                    else
                    {
                        var commands = bash
                            .Replace(Environment.NewLine, "\n").Split("\n", StringSplitOptions.RemoveEmptyEntries);

                        foreach (var command in commands)
                        {
                            sb.AppendLine(command);
                            var result = await connection.RunBash(command);
                            if(!string.IsNullOrEmpty(result.Output))
                                sb.AppendLine(result.Output);
                            if (result.ExitStatus != 0)
                            {
                                tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                                {
                                    Additional = new DeployAdditionalData()
                                    {
                                        Bash = bash,
                                        Error = result.Error,
                                        ExitStatus = result.ExitStatus,
                                        Output = sb.ToString().Replace(Environment.NewLine, "\n"),
                                        InProgress = false
                                    },
                                    Json = configuratorSettings.ToString(),
                                    Settings = configuratorSettings
                                });
                            }
                        }

                        tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                        {
                            Additional = new DeployAdditionalData()
                            {
                                Bash = bash,
                                Output = sb.ToString().Replace(Environment.NewLine, "\n"),
                                ExitStatus = 0,
                                InProgress = false
                            },
                            Json = configuratorSettings.ToString(),
                            Settings = configuratorSettings
                        });
                    }

                    await connection.DisconnectAsync();
                }
                catch (Exception e)
                {
                    tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                    {
                        Additional = new DeployAdditionalData()
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

        public UpdateSettings<ConfiguratorSettings, DeployAdditionalData> GetDeploymentResult(string id)
        {
            if (!OngoingDeployments.ContainsKey(id))
            {
                return null;
            }

            if (OngoingDeployments[id].Item1.IsCompleted)
            {
                return OngoingDeployments.Remove(id, out var result) ? result.Item1.Result : null;
            }

            return new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
            {
                Additional = new DeployAdditionalData()
                {
                    InProgress = true,
                    Output = OngoingDeployments[id].Item2.ToString().Replace(Environment.NewLine, "\n")
                }
            };
        }
    }
}