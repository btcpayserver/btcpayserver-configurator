using System.Collections.Generic;

namespace BTCPayServerDockerConfigurator.Models
{
    public class DeploymentAdditionalData
    {
        public List<DeploymentType> AvailableDeploymentTypes { get; set; } = new List<DeploymentType>();
    }
}