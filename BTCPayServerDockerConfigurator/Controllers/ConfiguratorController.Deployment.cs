using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("destination")]
        [HttpGet("")]
        public IActionResult DeploymentDestination(string password=null)
        {
            var model = GetConfiguratorSettings();

            model.DeploymentSettings.ThisMachinePassword = password;
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
                    var ssh = _options.Value.ParseSSHConfiguration(updateSettings.Settings.ThisMachinePassword);
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
            if (ssh == null)
            {
                return false;
            }
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

        private async Task<string> GetVar(Dictionary<string, string> dictionary, SshClient client, string name)
        {
            if (dictionary.ContainsKey(name))
            {
                return dictionary[name];
            }

            return await client.GetEnvVar(name);
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
                    sshSettings = _options.Value.ParseSSHConfiguration(settings.ThisMachinePassword);

                    break;
                }
            }

            using (var ssh = await sshSettings.ConnectAsync())
            {
                result.AdvancedSettings ??= new AdvancedSettings();
//                await ssh.RunBash(SSHClientExtensions.LoginAsRoot());

                var x = await ssh.RunBash("cat .env");
                Dictionary<string, string> preloadedEnvVars = new Dictionary<string, string>();
                if (x.ExitStatus == 0)
                {
                    preloadedEnvVars = x.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Split("=", StringSplitOptions.None)).ToDictionary(strings => strings[0],
                            strings => strings.Length > 1 ? strings[1] : "");
                }
                x = await ssh.RunBash("cat /etc/profile.d/btcpay-env.sh");
                if (x.ExitStatus == 0)
                {
                    preloadedEnvVars = x.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => s.Contains("=") && !s.Contains("==") && s.Contains("export"))
                        .Select(s => s.Split("=", StringSplitOptions.None)).ToDictionary(strings => strings[0].Replace("export ", ""),
                            strings => strings.Length > 1 ? strings[1].Trim('"') : "").Concat(preloadedEnvVars)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                }
                var branch = await
                    ssh.RunBash(
                        "if [ -d \"btcpayserver-docker\" ]; then git -C \"btcpayserver-docker\" branch | grep \\* | cut -d \" \"  -f2; fi");
                if (branch.ExitStatus == 0)
                {
                    result.AdvancedSettings.BTCPayDockerBranch = branch.Output;
                }

                var repo = await
                    ssh.RunBash(
                        "if [ -d \"btcpayserver-docker\" ]; then git -C \"btcpayserver-docker\" ls-remote --get-url;  fi");
                if (branch.ExitStatus == 0)
                {
                    result.AdvancedSettings.BTCPayDockerRepository = repo.Output;
                }


                result.AdvancedSettings.CustomBTCPayImage = await GetVar(preloadedEnvVars, ssh, "BTCPAY_IMAGE");
                result.AdvancedSettings.AdditionalFragments =
                    (await GetVar(preloadedEnvVars, ssh, "BTCPAYGEN_ADDITIONAL_FRAGMENTS"))
                    .Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                result.AdvancedSettings.ExcludedFragments =
                    (await GetVar(preloadedEnvVars, ssh, "BTCPAYGEN_EXCLUDE_FRAGMENTS"))
                    .Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();


                result.AdditionalServices ??= new AdditionalServices();
                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-librepatron"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-librepatron");
                    result.AdditionalServices.LibrePatronSettings.Enabled = true;
                    result.AdditionalServices.LibrePatronSettings.Host =
                        await GetVar(preloadedEnvVars, ssh, "LIBREPATRON_HOST");
                }

                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-woocommerce"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-woocommerce");
                    result.AdditionalServices.WooCommerceSettings.Enabled = true;
                    result.AdditionalServices.WooCommerceSettings.Host =
                        await GetVar(preloadedEnvVars, ssh, "WOOCOMMERCE_HOST");
                }

                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-btctransmuter"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-btctransmuter");
                    result.AdditionalServices.BTCTransmuterSettings.Enabled = true;
                    result.AdditionalServices.BTCTransmuterSettings.Host =
                        await GetVar(preloadedEnvVars, ssh, "BTCTRANSMUTER_HOST");
                }

                if (result.AdvancedSettings.AdditionalFragments.Contains("opt-add-tor-relay"))
                {
                    result.AdvancedSettings.AdditionalFragments.Remove("opt-add-tor-relay");
                    result.AdditionalServices.TorRelaySettings.Enabled = true;
                    result.AdditionalServices.TorRelaySettings.Nickname =
                        await GetVar(preloadedEnvVars, ssh, "TOR_RELAY_NICKNAME");
                    result.AdditionalServices.TorRelaySettings.Email =
                        await GetVar(preloadedEnvVars, ssh, "TOR_RELAY_EMAIL");
                }

                result.LightningSettings ??= new LightningSettings();
                result.LightningSettings.Implementation = await GetVar(preloadedEnvVars, ssh, "BTCPAYGEN_LIGHTNING");
                if (string.IsNullOrEmpty(result.LightningSettings.Implementation))
                {
                    result.LightningSettings.Implementation = "none";
                }
                result.LightningSettings.Alias = await GetVar(preloadedEnvVars, ssh, "LIGHTNING_ALIAS");

                var index = 1;
                result.ChainSettings ??= new ChainSettings();
                while (true)
                {
                    var chain = await GetVar(preloadedEnvVars, ssh, $"BTCPAYGEN_CRYPTO{index}");
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

                var matching =
                    result.AdvancedSettings.AdditionalFragments.FirstOrDefault(s => s.StartsWith("opt-save-storage"));
                if (string.IsNullOrEmpty(matching))
                {
                    result.ChainSettings.PruneMode = PruneMode.NoPruning;
                }
                else
                {
                    result.AdvancedSettings.AdditionalFragments.Remove(matching);
                    switch (matching.Replace("opt-save-storage", ""))
                    {
                        case "":
                            result.ChainSettings.PruneMode = PruneMode.Minimal;
                            break;
                        case "-s":
                            result.ChainSettings.PruneMode = PruneMode.Small;
                            break;
                        case "-xs":
                            result.ChainSettings.PruneMode = PruneMode.ExtraSmall;
                            break;
                        case "-xxs":
                            result.ChainSettings.PruneMode = PruneMode.ExtraExtraSmall;
                            break;
                    }
                }

                if (Enum.TryParse<NetworkType>(await GetVar(preloadedEnvVars, ssh, "NBITCOIN_NETWORK"), true,
                    out var networkType))
                {
                    result.ChainSettings.Network = networkType;
                }

                result.DomainSettings ??= new DomainSettings();
                result.DomainSettings.Domain = await GetVar(preloadedEnvVars, ssh, "BTCPAY_HOST");
                result.DomainSettings.AdditionalDomains =
                    (await GetVar(preloadedEnvVars, ssh, "BTCPAY_ADDITIONAL_HOSTS"))
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return result;
        }
    }
}