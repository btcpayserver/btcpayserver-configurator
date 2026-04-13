using System.ComponentModel.DataAnnotations;
using BTCPayServerDockerConfigurator.Validation;

namespace BTCPayServerDockerConfigurator.Models;

public class DeploymentSettings
{
    [Required] public DeploymentType DeploymentType { get; set; } = DeploymentType.ThisMachine;

    [RequiredIf(nameof(DeploymentType), nameof(DeploymentType.RemoteMachine),
        "Please enter the host/ip of the remote server")]
    public string Host { get; set; }

    [RequiredIf(nameof(DeploymentType), nameof(DeploymentType.RemoteMachine),
        "Please enter the username of the remote server")]
    public string Username { get; set; }

    public string Password { get; set; }
    public string RootPassword { get; set; }

    public SSHAuthMethod AuthMethod { get; set; } = SSHAuthMethod.Password;
    public string SSHPrivateKey { get; set; }
}

public enum SSHAuthMethod
{
    Password,
    SSHKey
}
