using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BTCPayServerDockerConfigurator.Models
{
    public class AdvancedSettings
    {
        [Display(Name = "Custom BTCPay Docker image")]
        public string CustomBTCPayImage { get; set; } = "";
        [Display(Name = "Custom btcpayserver-docker repository")]
        public string BTCPayDockerRepository { get; set; }
        [Display(Name = "Custom btcpayserver-docker branch")]
        public string BTCPayDockerBranch { get; set; }
        public List<string> AdditionalFragments { get; set; } = new List<string>();
        public List<string> ExcludedFragments { get; set; } = new List<string>();

        public bool AnythingSet()
        {
            return !string.IsNullOrEmpty(CustomBTCPayImage) || !string.IsNullOrEmpty(BTCPayDockerRepository) ||
                   !string.IsNullOrEmpty(BTCPayDockerBranch) || AdditionalFragments.Any() || ExcludedFragments.Any();
        }
    }
}