using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Models
{
    public static class SSHClientExtensions
    {
        public static async Task<SshClient> ConnectAsync(this SSHSettings sshSettings, CancellationToken cancellationToken = default)
        {
            if (sshSettings == null)
                throw new ArgumentNullException(nameof(sshSettings));
            TaskCompletionSource<SshClient> tcs = new TaskCompletionSource<SshClient>(TaskCreationOptions.RunContinuationsAsynchronously);
            new Thread(() =>
                {
                    SshClient sshClient = null;
                    try
                    {
                        sshClient = new SshClient(sshSettings.CreateConnectionInfo());
                        sshClient.HostKeyReceived += (object sender, Renci.SshNet.Common.HostKeyEventArgs e) =>
                        {
                            e.CanTrust = true;
                        };
                        sshClient.Connect();
                        tcs.TrySetResult(sshClient);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        try
                        {
                            sshClient?.Dispose();
                        }
                        catch { }
                    }
                })
                { IsBackground = true }.Start();

            using (cancellationToken.Register(() => { tcs.TrySetCanceled(); }))
            {
                return await tcs.Task;
            }
        }

        public static string EscapeSingleQuotes(this string command)
        {
            return command.Replace("'", "'\"'\"'", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<string> GetEnvVar(this SshClient sshClient, string name, TimeSpan? timeout = null)
        {
            var result =  await sshClient.RunBash($"echo \"${name}\"", timeout);
            if (string.IsNullOrEmpty(result.Error) && result.ExitStatus == 0)
            {
                return result.Output.Replace("\n", "").Replace(Environment.NewLine, "").Trim();
            }

            return "";

        }

        public static Task<SSHCommandResult> RunBash(this SshClient sshClient, string command, TimeSpan? timeout = null)
        {
            if (sshClient == null)
                throw new ArgumentNullException(nameof(sshClient));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            command = $"bash -c '{command.EscapeSingleQuotes()}'";
            var sshCommand = sshClient.CreateCommand(command);
            if (timeout is TimeSpan v)
                sshCommand.CommandTimeout = v;
            var tcs = new TaskCompletionSource<SSHCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            
            
            new Thread(async () =>
                {
                    
                    var asyncResult  = sshCommand.BeginExecute(ar =>
                    {
                        try
                        {
                            
                            sshCommand.EndExecute(ar);
                            tcs.TrySetResult(CreateSSHCommandResult(sshCommand));
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                        finally
                        {
                            sshCommand.Dispose();
                        }
                    });
                })
                { IsBackground = false }.Start();
            return tcs.Task;
        }

        
        

        private static SSHCommandResult CreateSSHCommandResult(SshCommand sshCommand)
        {
            return new SSHCommandResult()
            {
                Output = sshCommand.Result,
                Error = sshCommand.Error,
                ExitStatus = sshCommand.ExitStatus
            };
        }

        public static async Task DisconnectAsync(this SshClient sshClient, CancellationToken cancellationToken = default)
        {
            if (sshClient == null)
                throw new ArgumentNullException(nameof(sshClient));
            
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            new Thread(() =>
                {
                    try
                    {
                        sshClient.Disconnect();
                        tcs.TrySetResult(true);
                    }
                    catch
                    {
                        tcs.TrySetResult(true); // We don't care about exception
                    }
                })
                { IsBackground = true }.Start();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }
        }
        
        public class SSHCommandResult
        {
            public int ExitStatus { get; internal set; }
            public string Output { get; internal set; }
            public string Error { get; internal set; }
        }
        
        public static string TrimEnd(this string input, string suffixToRemove,
            StringComparison comparisonType) {

            if (input != null && suffixToRemove != null
                              && input.EndsWith(suffixToRemove, comparisonType)) {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }
            else return input;
        }
    }
}