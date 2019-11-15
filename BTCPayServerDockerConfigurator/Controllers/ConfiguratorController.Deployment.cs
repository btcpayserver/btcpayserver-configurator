using System;
using System.Text.Json;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("destination")]
        [HttpGet("")]
        public IActionResult DeploymentDestination()
        {
            var model = GetConfiguratorSettings();


            return View(new UpdateSettings<DeploymentSettings, DeploymentAdditionalData>()
            {
                Json = JsonSerializer.Serialize(model),
                Settings = model.DeploymentSettings,
                Additional = ConstructDeploymentAdditionalData()
            });
        }

        [HttpPost("destination")]
        public async Task<IActionResult> DeploymentDestination(
            UpdateSettings<DeploymentSettings, DeploymentAdditionalData> updateSettings)
        {
            updateSettings.Additional = ConstructDeploymentAdditionalData();
            switch (updateSettings.Settings.DeploymentType)
            {
                case DeploymentType.RemoteMachine when ModelState.IsValid:
                {
                    var ssh = new SSHSettings()
                    {
                        Password = updateSettings.Settings.Password,
                        Server = updateSettings.Settings.Host,
                        Username = updateSettings.Settings.Username
                    };

                    if (!await TestSSH(ssh))
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Host),
                            "Could not connect with specified SSH details");
                    }

                    break;
                }
                case DeploymentType.ThisMachine when ModelState.IsValid:
                {
                    var ssh = _options.Value.ParseSSHConfiguration();
                    if (!await TestSSH(ssh))
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.DeploymentType),
                            "Couldn't SSH into the host. That's bad fyi.'");
                    }

                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(updateSettings);
            }

            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);
            configuratorSettings.DeploymentSettings = updateSettings.Settings;
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(DomainSettings));
        }

        private async Task<bool> TestSSH(SSHSettings ssh)
        {
            try
            {
                var test = await ssh.ConnectAsync();
                if (test.IsConnected)
                {
                    await test.DisconnectAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private DeploymentAdditionalData ConstructDeploymentAdditionalData()
        {
            var additionalData = new DeploymentAdditionalData();
            if (!string.IsNullOrEmpty(_options.Value.SSHConnection))
            {
                additionalData.AvailableDeploymentTypes.Add(DeploymentType.ThisMachine);
            }

            additionalData.AvailableDeploymentTypes.Add(DeploymentType.RemoteMachine);
            additionalData.AvailableDeploymentTypes.Add(DeploymentType.Manual);
            return additionalData;
        }
    }
}