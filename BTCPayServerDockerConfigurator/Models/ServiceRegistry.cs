using System.Text;

namespace BTCPayServerDockerConfigurator.Models;

public class ServiceDefinition
{
    public string FragmentName { get; init; }
    public Func<AdditionalServices, bool> IsEnabled { get; init; }
    public Action<AdditionalServices, bool> SetEnabled { get; init; }
    public Dictionary<string, Func<AdditionalServices, string>> EnvVars { get; init; } = new();
    public Dictionary<string, Action<AdditionalServices, string>> LoadEnvVars { get; init; } = new();

    public void ApplyToScript(StringBuilder sb, List<string> fragments, AdditionalServices svc)
    {
        if (!IsEnabled(svc)) return;
        fragments.Add(FragmentName);
        foreach (var (envName, getter) in EnvVars)
        {
            var val = getter(svc);
            if (!string.IsNullOrEmpty(val))
                sb.AppendLine($"export {envName}=\"{val}\"");
        }
    }

    public void LoadFromFragments(List<string> fragments, AdditionalServices svc,
        Dictionary<string, string> envVars)
    {
        if (!fragments.Contains(FragmentName)) return;
        fragments.Remove(FragmentName);
        SetEnabled(svc, true);
        foreach (var (envName, setter) in LoadEnvVars)
        {
            if (envVars.TryGetValue(envName, out var val))
                setter(svc, val);
        }
    }
}

public static class ServiceRegistry
{
    public static readonly List<ServiceDefinition> All =
    [
        new ServiceDefinition
        {
            FragmentName = "opt-add-librepatron",
            IsEnabled = s => s.LibrePatronSettings.Enabled,
            SetEnabled = (s, v) => s.LibrePatronSettings.Enabled = v,
            EnvVars = new()
            {
                ["LIBREPATRON_HOST"] = s => s.LibrePatronSettings.Host
            },
            LoadEnvVars = new()
            {
                ["LIBREPATRON_HOST"] = (s, v) => s.LibrePatronSettings.Host = v
            }
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-woocommerce",
            IsEnabled = s => s.WooCommerceSettings.Enabled,
            SetEnabled = (s, v) => s.WooCommerceSettings.Enabled = v,
            EnvVars = new()
            {
                ["WOOCOMMERCE_HOST"] = s => s.WooCommerceSettings.Host
            },
            LoadEnvVars = new()
            {
                ["WOOCOMMERCE_HOST"] = (s, v) => s.WooCommerceSettings.Host = v
            }
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-btctransmuter",
            IsEnabled = s => s.BTCTransmuterSettings.Enabled,
            SetEnabled = (s, v) => s.BTCTransmuterSettings.Enabled = v
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-configurator",
            IsEnabled = s => s.ConfiguratorAddonSettings.Enabled,
            SetEnabled = (s, v) => s.ConfiguratorAddonSettings.Enabled = v
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-electrum-ps",
            IsEnabled = s => s.ElectrumPersonalServerSettings.Enabled,
            SetEnabled = (s, v) => s.ElectrumPersonalServerSettings.Enabled = v,
            EnvVars = new()
            {
                ["EPS_XPUB"] = s => s.ElectrumPersonalServerSettings.Xpub
            },
            LoadEnvVars = new()
            {
                ["EPS_XPUB"] = (s, v) => s.ElectrumPersonalServerSettings.Xpub = v
            }
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-electrumx",
            IsEnabled = s => s.ElectrumXSettings.Enabled,
            SetEnabled = (s, v) => s.ElectrumXSettings.Enabled = v
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-thunderhub",
            IsEnabled = s => s.ThunderHubSettings.Enabled,
            SetEnabled = (s, v) => s.ThunderHubSettings.Enabled = v
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-tor-relay",
            IsEnabled = s => s.TorRelaySettings.Enabled,
            SetEnabled = (s, v) => s.TorRelaySettings.Enabled = v,
            EnvVars = new()
            {
                ["TOR_RELAY_NICKNAME"] = s => s.TorRelaySettings.Nickname,
                ["TOR_RELAY_EMAIL"] = s => s.TorRelaySettings.Email
            },
            LoadEnvVars = new()
            {
                ["TOR_RELAY_NICKNAME"] = (s, v) => s.TorRelaySettings.Nickname = v,
                ["TOR_RELAY_EMAIL"] = (s, v) => s.TorRelaySettings.Email = v
            }
        },
        new ServiceDefinition
        {
            FragmentName = "opt-add-pihole",
            IsEnabled = s => s.PiHoleSettings.Enabled,
            SetEnabled = (s, v) => s.PiHoleSettings.Enabled = v,
            EnvVars = new()
            {
                ["PIHOLE_SERVERIP"] = s => s.PiHoleSettings.ServerIp
            },
            LoadEnvVars = new()
            {
                ["PIHOLE_SERVERIP"] = (s, v) => s.PiHoleSettings.ServerIp = v
            }
        }
    ];
}
