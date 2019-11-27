using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Controllers;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.Options;
using Renci.SshNet;
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

        public string StartDeployment(ConfiguratorSettings configuratorSettings)
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


                    using var connection = await ssh.ConnectAsync();
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
                      var shell = connection.CreateShellStream("xterm", 50, 50, 640, 480, 17640);
                        var commands = bash
                            .Replace(Environment.NewLine, "\n").Split("\n", StringSplitOptions.RemoveEmptyEntries);

  

                        var result = await RunCommandsInShellSequencial(commands, shell, sb);
                        var exitcode = result.Item1 ? 0 : -1;


                        tcs.SetResult(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                                {
                                    Additional = new DeployAdditionalData()
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

        private async Task<(bool, string)> RunCommandsInShellSequencial(string[] commands, ShellStream shellStream, StringBuilder result)
        {
            var failed = false;
            var cts = new TaskCompletionSource<bool>();
            shellStream.ErrorOccurred += (sender, args) => {
                result.AppendLine(args.Exception.Message);
               
                failed = true;
                cts.SetResult(false);
            };
            shellStream.DataReceived += (sender, args) =>
            {
                var str = Encoding.UTF8.GetString(args.Data);
                result.Append(str);
                if(!str.Contains("echo \"eolcomment\"") && str.Contains("eolcomment"))
                {
                    cts.SetResult(true);
                }
            };
            var y = shellStream.Read();
            foreach (var command in commands)
            {
                if (failed)
                {
                    break;
                }
                shellStream.WriteLine(command);
                shellStream.ReadLine();
            }
            
            shellStream.WriteLine("echo \"eolcomment\"");
           failed= !await cts.Task;
            return (!failed, result.ToString());
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