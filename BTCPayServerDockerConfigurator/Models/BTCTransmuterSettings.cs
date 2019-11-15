using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class BTCTransmuterSettings
    {
        [RequiredIf(nameof(Enabled), "true", "A host must be set when enabled")]
        [Display(Name = "Hostname of your BTC Transmuter website")]
        public string Host { get; set; }
        public bool Enabled { get; set; }
    }
}