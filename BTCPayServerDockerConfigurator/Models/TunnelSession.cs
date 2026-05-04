using System.Threading.Channels;

namespace BTCPayServerDockerConfigurator.Models;

public enum TunnelState
{
    WaitingForAgent,
    Connected,
    Loading,
    Done,
    Error,
    Expired
}

public class TunnelSession : IRemoteExecutor
{
    public string Secret { get; }
    public TunnelState State { get; set; } = TunnelState.WaitingForAgent;
    public ConfiguratorSettings LoadedSettings { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    private readonly Channel<string> _commandChannel = Channel.CreateBounded<string>(1);
    private readonly Channel<SSHClientExtensions.SSHCommandResult> _resultChannel =
        Channel.CreateBounded<SSHClientExtensions.SSHCommandResult>(1);
    private readonly TaskCompletionSource _agentConnected = new();

    public TunnelSession(string secret) => Secret = secret;

    public async Task<SSHClientExtensions.SSHCommandResult> RunBash(string command,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(60);
        using var cts = new CancellationTokenSource(timeout.Value);

        try
        {
            await _commandChannel.Writer.WriteAsync(command, cts.Token);
            return await _resultChannel.Reader.ReadAsync(cts.Token);
        }
        catch (ChannelClosedException)
        {
            return new SSHClientExtensions.SSHCommandResult
            {
                Output = "",
                Error = ErrorMessage ?? "Agent disconnected",
                ExitStatus = -1
            };
        }
    }

    public async Task<string> GetNextCommand(CancellationToken ct)
    {
        LastActivity = DateTime.UtcNow;

        if (State == TunnelState.WaitingForAgent)
        {
            State = TunnelState.Connected;
            _agentConnected.TrySetResult();
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(25));

        try
        {
            return await _commandChannel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task SetResult(string output, int exitCode, CancellationToken ct = default)
    {
        LastActivity = DateTime.UtcNow;
        await _resultChannel.Writer.WriteAsync(new SSHClientExtensions.SSHCommandResult
        {
            Output = output?.TrimEnd('\n') ?? "",
            Error = "",
            ExitStatus = exitCode
        }, ct);
    }

    public void OnDisconnect()
    {
        State = TunnelState.Error;
        ErrorMessage = "Agent disconnected";
        _agentConnected.TrySetResult();
        _commandChannel.Writer.TryComplete(new OperationCanceledException("Agent disconnected"));
        _resultChannel.Writer.TryComplete(new OperationCanceledException("Agent disconnected"));
    }

    public Task WaitForAgent(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => _agentConnected.TrySetCanceled());
        return _agentConnected.Task;
    }

    public void Complete()
    {
        _commandChannel.Writer.TryComplete();
        _resultChannel.Writer.TryComplete();
    }

    public void Dispose()
    {
        Complete();
    }
}
