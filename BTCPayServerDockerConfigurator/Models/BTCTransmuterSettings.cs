using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class BTCTransmuterSettings
    {
        [Display(Name = "Hostname of your BTC Transmuter website(optional)")]
        public string Host { get; set; }
        public bool Enabled { get; set; }
    }
}