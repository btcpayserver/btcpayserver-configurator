using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Models
{
    public class ChainSettings
    {
        public bool Bitcoin { get; set; } = true;
        [Display(Name = "")] public List<string> AltChains { get; set; } = new List<string>();
        [Display(Name = "Pruning mode")] public PruneMode PruneMode { get; set; } = PruneMode.Small;

        public NetworkType Network { get; set; } = NetworkType.Mainnet;
    }
}