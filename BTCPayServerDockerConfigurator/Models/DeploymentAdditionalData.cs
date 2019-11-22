using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Models
{
    public class DeploymentAdditionalData
    {
        public List<DeploymentType> AvailableDeploymentTypes { get; set; } = new List<DeploymentType>();
        [Display(Name = "Load existing settings(if possible)")]
        public bool LoadFromServer { get; set; } = true;
    }
}