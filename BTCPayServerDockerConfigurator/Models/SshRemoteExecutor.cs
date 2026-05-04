using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Models;

public class SshRemoteExecutor : IRemoteExecutor
{
    private readonly SshClient _client;

    public SshRemoteExecutor(SshClient client) => _client = client;

    public SshClient Client => _client;

    public Task<SSHClientExtensions.SSHCommandResult> RunBash(string command,
        TimeSpan? timeout = null)
        => _client.RunBash(command, timeout);

    public void Dispose() => _client.Dispose();
}
