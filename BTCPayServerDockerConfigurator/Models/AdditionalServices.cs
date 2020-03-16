namespace BTCPayServerDockerConfigurator.Models
{
    public class AdditionalServices
    {
        public LibrePatronSettings LibrePatronSettings { get; set; } = new LibrePatronSettings();
        public BTCTransmuterSettings BTCTransmuterSettings { get; set; } = new BTCTransmuterSettings();
        public ConfiguratorAddonSettings ConfiguratorAddonSettings { get; set; } = new ConfiguratorAddonSettings();
        public WooCommerceSettings WooCommerceSettings { get; set; } = new WooCommerceSettings();
        public TorRelaySettings TorRelaySettings { get; set; } = new TorRelaySettings();
    }
}