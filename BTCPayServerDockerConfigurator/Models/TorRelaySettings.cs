using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class TorRelaySettings
    {
        [RequiredIf(nameof(Enabled), "True", "Required")]
        [Display(Name = "Relay nickname")]
        public string Nickname { get; set; }
        [RequiredIf(nameof(Enabled), "True", "Required")]
        [EmailAddress]
        [Display(Name = "Tor contact email")]
        public string Email { get; set; }
        public bool Enabled { get; set; }
    }
}