using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Models
{
    public class PiHoleSettings
    {
        public bool Enabled { get; set; }

        [Display(Name = "IP of server, to enable Pi-Hole dashboard")]
        public string ServerIp { get; set; }
    }
}