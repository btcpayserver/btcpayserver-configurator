namespace BTCPayServerDockerConfigurator.Models;

public class AdditionalServices
{
    public LibrePatronSettings LibrePatronSettings { get; set; } = new();
    public BTCTransmuterSettings BTCTransmuterSettings { get; set; } = new();
    public ConfiguratorAddonSettings ConfiguratorAddonSettings { get; set; } = new();
    public WooCommerceSettings WooCommerceSettings { get; set; } = new();
    public TorRelaySettings TorRelaySettings { get; set; } = new();
    public ElectrumPersonalServerSettings ElectrumPersonalServerSettings { get; set; } = new();
    public ElectrumXSettings ElectrumXSettings { get; set; } = new();
    public ThunderHubSettings ThunderHubSettings { get; set; } = new();
    public PiHoleSettings PiHoleSettings { get; set; } = new();
}
