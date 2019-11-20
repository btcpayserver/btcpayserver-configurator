using System;
using System.Collections.Generic;
using System.Linq;
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

            ConfiguratorSettings configuratorSettings;
            if (updateSettings.Settings.DeploymentType != DeploymentType.Manual)
            {
                configuratorSettings = await LoadSettingsThroughSSH(updateSettings.Settings);
            }
            else
            {
                configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                    ? new ConfiguratorSettings()
                    : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);
                configuratorSettings.DeploymentSettings = updateSettings.Settings;
            }

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

        public async Task<ConfiguratorSettings> LoadSettingsThroughSSH(DeploymentSettings settings)
        {
            SSHSettings sshSettings = null;
            var result = new ConfiguratorSettings()
            {
                DeploymentSettings = settings
            };
            switch (settings.DeploymentType)
            {
                case DeploymentType.RemoteMachine when ModelState.IsValid:
                {
                    sshSettings = new SSHSettings()
                    {
                        Password = settings.Password,
                        Server = settings.Host,
                        Username = settings.Username
                    };

                    break;
                }
                case DeploymentType.ThisMachine when ModelState.IsValid:
                {
                    sshSettings =  _options.Value.ParseSSHConfiguration();

                    break;
                }
            }
            using(var ssh = await sshSettings.ConnectAsync())
            {
                result.AdvancedSettings ??= new AdvancedSettings();
//                await ssh.RunBash(SSHClientExtensions.LoginAsRoot());
                
                var branch = await
                    ssh.RunBash(
                        "if [ -d \"btcpayserver-docker\" ]; then git -C \"btcpayserver-docker\" branch | grep \\* | cut -d \" \"  -f2; fi");
                if (branch.ExitStatus == 0)
                {
                    result.AdvancedSettings.BTCPayDockerBranch = branch.Output;
                }
                var repo =await
                    ssh.RunBash(
                        "if [ -d \"btcpayserver-docker\" ]; then git -C \"btcpayserver-docker\" ls-remote --get-url;  fi");
                if (branch.ExitStatus == 0)
                {
                    result.AdvancedSettings.BTCPayDockerRepository = repo.Output;
                }
                result.AdvancedSettings.CustomBTCPayImage = await ssh.GetEnvVar("BTCPAY_IMAGE");
                result.AdvancedSettings.AdditionalFragments = (await ssh.GetEnvVar("BTCPAYGEN_ADDITIONAL_FRAGMENTS"))
                    .Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                result.AdvancedSettings.ExcludedFragments = (await ssh.GetEnvVar("BTCPAYGEN_EXCLUDE_FRAGMENTS"))
                    .Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                

                
                result.AdditionalServices ??= new AdditionalServices();
                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-librepatron"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-librepatron");
                    result.AdditionalServices.LibrePatronSettings.Enabled = true;
                    result.AdditionalServices.LibrePatronSettings.Host = await ssh.GetEnvVar("LIBREPATRON_HOST");
                }
                
                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-woocommerce"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-woocommerce");
                    result.AdditionalServices.WooCommerceSettings.Enabled = true;
                    result.AdditionalServices.WooCommerceSettings.Host = await ssh.GetEnvVar("WOOCOMMERCE_HOST");
                }
                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-btctransmuter"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-btctransmuter");
                    result.AdditionalServices.BTCTransmuterSettings.Enabled = true;
                    result.AdditionalServices.BTCTransmuterSettings.Host = await ssh.GetEnvVar("BTCTRANSMUTER_HOST");
                }
                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-tor-relay"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-tor-relay");
                    result.AdditionalServices.TorRelaySettings.Enabled = true;
                    result.AdditionalServices.TorRelaySettings.Nickname =  await ssh.GetEnvVar("TOR_RELAY_NICKNAME");
                    result.AdditionalServices.TorRelaySettings.Email =  await ssh.GetEnvVar("TOR_RELAY_EMAIL");
                }
                
                result.LightningSettings ??= new LightningSettings();
                result.LightningSettings.Implementation  = await ssh.GetEnvVar("BTCPAYGEN_LIGHTNING");
                result.LightningSettings.Alias  = await ssh.GetEnvVar("LIGHTNING_ALIAS");
                
                var index = 1;
                result.ChainSettings ??= new ChainSettings();
                while (true)
                {
                    var chain = await ssh.GetEnvVar($"BTCPAYGEN_CRYPTO{index}");
                    if (string.IsNullOrEmpty(chain))
                    {
                        break;
                    }

                    if (chain.Equals("btc", StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.ChainSettings.Bitcoin = true;
                    }
                    else
                    {
                        result.ChainSettings.AltChains.Add(chain);
                    }
                    index++;
                }

                var matching = result.AdvancedSettings.AdditionalFragments.FirstOrDefault(s => s.StartsWith("opt-save-storage"));
                if (string.IsNullOrEmpty(matching))
                {
                    result.ChainSettings.PruneMode = PruneMode.NoPruning;
                }
                else
                {
                    result.AdvancedSettings.AdditionalFragments.Remove(matching);
                    switch (matching.Replace("opt-save-storage-", ""))
                    {
                        case "":
                            result.ChainSettings.PruneMode = PruneMode.Minimal;
                            break;
                        case "s":
                            result.ChainSettings.PruneMode = PruneMode.Small;
                            break;
                        case "xs":
                            result.ChainSettings.PruneMode = PruneMode.ExtraSmall;
                            break;
                        case "xxs":
                            result.ChainSettings.PruneMode = PruneMode.ExtraExtraSmall;
                            break;
                    }
                }

                if (Enum.TryParse<NetworkType>(await ssh.GetEnvVar("NBITCOIN_NETWORK"), true, out var networkType))
                {
                    result.ChainSettings.Network = networkType;
                }

                result.DomainSettings ??= new DomainSettings();
                result.DomainSettings.Domain = await ssh.GetEnvVar("BTCPAY_HOST");
                result.DomainSettings.AdditionalDomains =(await ssh.GetEnvVar("BTCPAY_ADDITIONAL_HOSTS"))
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return result;

        }
       
        
    }
}