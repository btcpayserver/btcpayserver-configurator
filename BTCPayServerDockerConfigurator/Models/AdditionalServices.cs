namespace BTCPayServerDockerConfigurator.Models
{
    public class AdditionalServices
    {
        public LibrePatronSettings LibrePatronSettings { get; set; }
        public BTCTransmuterSettings BTCTransmuterSettings { get; set; }
        public WooCommerceSettings WooCommerceSettings { get; set; }
        public TorRelaySettings TorRelaySettings { get; set; }
    }
}