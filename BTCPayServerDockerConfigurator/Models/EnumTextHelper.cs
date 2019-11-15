using System.Collections.Generic;

namespace BTCPayServerDockerConfigurator.Models
{
    public static class EnumTextHelper
    {
        public static Dictionary<PruneMode, string> PruneMode = new Dictionary<PruneMode, string>()
        {
            {Models.PruneMode.NoPruning, "No pruning"},
            {Models.PruneMode.Minimal, "100GB ~1 year worth of blocks"},
            {Models.PruneMode.Small, "50GB ~6 months worth of blocks"},
            {Models.PruneMode.ExtraSmall, "25GB ~3 months worth of blocks"},
            {Models.PruneMode.ExtraExtraSmall, "5GB ~2 weeks worth of blocks"},
        };
    }
}