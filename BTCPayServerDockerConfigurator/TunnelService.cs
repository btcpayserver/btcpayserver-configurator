using System.Collections.Concurrent;
using BTCPayServerDockerConfigurator.Models;

namespace BTCPayServerDockerConfigurator;

public class TunnelService : IHostedService
{
    private readonly ConcurrentDictionary<string, TunnelSession> _sessions = new();
    private Timer _cleanupTimer;

    public TunnelSession CreateSession()
    {
        var secret = Guid.NewGuid().ToString("N");
        var session = new TunnelSession(secret);
        _sessions.TryAdd(secret, session);
        return session;
    }

    public TunnelSession GetSession(string secret)
    {
        return _sessions.TryGetValue(secret, out var session) ? session : null;
    }

    public void RemoveSession(string secret)
    {
        if (_sessions.TryRemove(secret, out var session))
        {
            session.Dispose();
        }
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cleanupTimer = new Timer(_ =>
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-10);
            foreach (var kvp in _sessions)
            {
                if (kvp.Value.LastActivity < cutoff)
                {
                    kvp.Value.State = TunnelState.Expired;
                    RemoveSession(kvp.Key);
                }
            }
        }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _cleanupTimer?.Dispose();
        foreach (var kvp in _sessions)
            kvp.Value.Dispose();
        _sessions.Clear();
        return Task.CompletedTask;
    }
}
