namespace BTCPayServerDockerConfigurator.Models
{
    public class AdditionalServices
    {
        public LibrePatronSettings LibrePatronSettings { get; set; } = new LibrePatronSettings();
        public BTCTransmuterSettings BTCTransmuterSettings { get; set; } = new BTCTransmuterSettings();
        public ConfiguratorAddonSettings ConfiguratorAddonSettings { get; set; } = new ConfiguratorAddonSettings();
        public WooCommerceSettings WooCommerceSettings { get; set; } = new WooCommerceSettings();
        public TorRelaySettings TorRelaySettings { get; set; } = new TorRelaySettings();

        public ElectrumPersonalServerSettings ElectrumPersonalServerSettings { get; set; } =
            new ElectrumPersonalServerSettings();

        public ElectrumXSettings ElectrumXSettings { get; set; } =
            new ElectrumXSettings();

        public ThunderHubSettings ThunderHubSettings { get; set; } =
            new ThunderHubSettings();
    }
}