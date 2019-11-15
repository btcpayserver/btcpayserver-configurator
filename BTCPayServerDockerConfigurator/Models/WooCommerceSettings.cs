using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class WooCommerceSettings
    {
        [RequiredIf(nameof(Enabled), "true", "A host must be set when enabled")]
        [Display(Name = "Hostname of your WooCommerce website")]
        public string Host { get; set; }
        public bool Enabled { get; set; }
    }
}