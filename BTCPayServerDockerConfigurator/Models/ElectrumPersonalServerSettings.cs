using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class ElectrumPersonalServerSettings
    {
        public bool Enabled { get; set; }

        [RequiredIf(nameof(Enabled), "True", "Required")]
        [Display(Name = "XPUB")]
        public string Xpub { get; set; }
    }
}