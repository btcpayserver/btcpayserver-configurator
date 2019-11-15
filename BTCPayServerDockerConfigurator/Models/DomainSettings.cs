using System.Collections.Generic;

namespace BTCPayServerDockerConfigurator.Models
{
    public class DomainSettings
    {
        public string Domain { get; set; }
        public List<string> AdditionalDomains { get; set; } = new List<string>();
    }
}