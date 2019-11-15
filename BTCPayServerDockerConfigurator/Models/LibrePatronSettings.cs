using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class LibrePatronSettings
    {
        [RequiredIf(nameof(Enabled), "true", "A host must be set when enabled")]
        [Display(Name = "Hostname of your Libre Patron website")]
        public string Host { get; set; }
        public bool Enabled { get; set; }
    }
}