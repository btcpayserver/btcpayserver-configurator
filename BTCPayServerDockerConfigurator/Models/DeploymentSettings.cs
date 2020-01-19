using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models
{
    public class DeploymentSettings
    {
        [Required] public DeploymentType DeploymentType { get; set; }

        [RequiredIf(nameof(DeploymentType), nameof(DeploymentType.RemoteMachine),
            "Please enter the host/ip of the remote server")]
        public string Host { get; set; }

        [RequiredIf(nameof(DeploymentType), nameof(DeploymentType.RemoteMachine),
            "Please enter the username of the remote server")]
        public string Username { get; set; }

        [RequiredIf(nameof(DeploymentType), nameof(DeploymentType.RemoteMachine),
            "Please enter the password of the remote server")]
        public string Password { get; set; }
        public string RootPassword { get; set; }
    }
}