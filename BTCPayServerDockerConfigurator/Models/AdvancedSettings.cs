using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Models;

public class AdvancedSettings
{
    [Display(Name = "Custom BTCPay Docker image")]
    public string CustomBTCPayImage { get; set; } = "";

    [Display(Name = "Custom btcpayserver-docker repository")]
    public string BTCPayDockerRepository { get; set; }

    [Display(Name = "Custom btcpayserver-docker branch")]
    public string BTCPayDockerBranch { get; set; }

    public List<string> AdditionalFragments { get; set; } = new();
    public List<string> ExcludedFragments { get; set; } = new();

    [Display(Name = "Enable FastSync (speeds up initial Bitcoin sync)")]
    public bool FastSync { get; set; }

    public bool AnythingSet()
    {
        return !string.IsNullOrEmpty(CustomBTCPayImage) || !string.IsNullOrEmpty(BTCPayDockerRepository) ||
               !string.IsNullOrEmpty(BTCPayDockerBranch) || AdditionalFragments.Any() ||
               ExcludedFragments.Any() || FastSync;
    }

    public void EnsureTxIndex()
    {
        var fragment = "opt-txindex";
        if (AdditionalFragments.Contains(fragment))
            return;
        AdditionalFragments.Add(fragment);
    }
}
