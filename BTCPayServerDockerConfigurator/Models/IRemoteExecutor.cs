namespace BTCPayServerDockerConfigurator.Models;

public interface IRemoteExecutor : IDisposable
{
    Task<SSHClientExtensions.SSHCommandResult> RunBash(string command, TimeSpan? timeout = null);
}

public static class RemoteExecutorExtensions
{
    public static async Task<string> GetEnvVar(this IRemoteExecutor executor, string name,
        TimeSpan? timeout = null)
    {
        var result = await executor.RunBash($"echo \"${name}\"", timeout);
        if (string.IsNullOrEmpty(result.Error) && result.ExitStatus == 0)
        {
            return result.Output.Replace("\n", "").Replace(Environment.NewLine, "").Trim();
        }

        return "";
    }
}
