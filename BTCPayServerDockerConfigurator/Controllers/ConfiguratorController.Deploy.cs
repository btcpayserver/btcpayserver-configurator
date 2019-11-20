using System;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("onion")]
        public async Task<IActionResult> GetOnionUrl()
        {
            var ssh = GetSshSettings(GetConfiguratorSettings());
            using (var sshC = await ssh.ConnectAsync())
            {

                {
                    var result =
                        await sshC.RunBash("cat /var/lib/docker/volumes/generated_tor_servicesdir/_data/BTCPayServer/hostname");
                    return View(result);
                }
            }
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> Deploy()
        {
            var model = GetConfiguratorSettings();
            var bash = model.ConstructBashFile(null);
            var oneliner = bash
                .Replace(Environment.NewLine, "\n")
                .Replace("\n", " && \n")
                .TrimEnd(" && \n", StringComparison.InvariantCultureIgnoreCase);

            if (model.DeploymentSettings.DeploymentType == DeploymentType.Manual)
            {
                return View(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                {
                    Additional = new DeployAdditionalData()
                    {
                        Bash = bash
                    },
                    Json = model.ToString(),
                    Settings = model
                });
            }
            var ssh = GetSshSettings(model);

            try
            {

                var connection = await ssh.ConnectAsync();
                
                var result = await connection.RunBash(oneliner.Replace("\n", ""));
                
                return View(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                {
                    Additional = new DeployAdditionalData()
                    {
                        Bash = bash,
                        Error = result.Error,
                        Output = result.Output,
                        ExitStatus = result.ExitStatus
                    },
                    Json = model.ToString(),
                    Settings = model
                });

            }
            catch (Exception e)
            {
                return View(new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>()
                {
                    Additional = new DeployAdditionalData()
                    {
                        Bash = bash,
                        Error = e.Message
                    },
                    Json = model.ToString(),
                    Settings = model
                });
            }

        }

        private SSHSettings GetSshSettings(ConfiguratorSettings model)
        {
            SSHSettings ssh = null;
            ;
            switch (model.DeploymentSettings.DeploymentType)
            {
                case DeploymentType.RemoteMachine when ModelState.IsValid:
                {
                    ssh = new SSHSettings()
                    {
                        Password = model.DeploymentSettings.Password,
                        Server = model.DeploymentSettings.Host,
                        Username = model.DeploymentSettings.Username
                    };
                    break;
                }
                case DeploymentType.ThisMachine when ModelState.IsValid:
                {
                    ssh = _options.Value.ParseSSHConfiguration();
                    break;
                }
            }

            if (ssh == null)
            {
                throw new Exception("lolita bonita");
            }

            return ssh;
        }
    }

    public class DeployAdditionalData: SSHClientExtensions.SSHCommandResult
    {
        public string Bash { get; set; }
    }
}