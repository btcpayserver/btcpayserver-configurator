using System;
using System.Text.Json;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("additional")]
        public IActionResult AdditionalServices()
        {
            var model = GetConfiguratorSettings();

            return View(new UpdateSettings<AdditionalServices, AdditionalDataStub>()
            {
                Json = model.ToString(),
                Settings = model.AdditionalServices
            });
        }

        [HttpPost("additional")]
        public IActionResult AdditionalServices(
            UpdateSettings<AdditionalServices, AdditionalDataStub> updateSettings)
        {
            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);

            if (ModelState.IsValid)
            {
                if (updateSettings.Settings.LibrePatronSettings.Enabled)
                {
                    var error = CheckHost(updateSettings.Settings.LibrePatronSettings.Host,
                        configuratorSettings);
                    if (!string.IsNullOrEmpty(error))
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.LibrePatronSettings) + "." +
                            nameof(updateSettings.Settings.LibrePatronSettings.Host),
                            error);
                    }
                }

                if (updateSettings.Settings.ElectrumPersonalServerSettings.Enabled)
                {
                    if (!configuratorSettings.ChainSettings.Bitcoin)
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.ElectrumPersonalServerSettings) + "." +
                            nameof(updateSettings.Settings.ElectrumPersonalServerSettings.Enabled),
                            "This can only be used if Bitcoin was enabled.");
                    }

                    //TODO: add xpub validation here
                }

                if (updateSettings.Settings.ElectrumXSettings.Enabled)
                {
                    if (!configuratorSettings.ChainSettings.Bitcoin)
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings.Enabled),
                            "This can only be used if Bitcoin was enabled.");
                    }
                    else if (configuratorSettings.ChainSettings.PruneMode != PruneMode.NoPruning)
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings.Enabled),
                            "ElectrumX can only be used with a non-pruned node.");
                    }
                }

                if (updateSettings.Settings.WooCommerceSettings.Enabled)
                {
                    var error = CheckHost(updateSettings.Settings.WooCommerceSettings.Host,
                        configuratorSettings);
                    if (!string.IsNullOrEmpty(error))
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.WooCommerceSettings) + "." +
                            nameof(updateSettings.Settings.WooCommerceSettings.Host),
                            error);
                    }
                }

                if (updateSettings.Settings.ThunderHubSettings.Enabled)
                {
                    if (!configuratorSettings.ChainSettings.Bitcoin)
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings) + "." +
                            nameof(updateSettings.Settings.ElectrumXSettings.Enabled),
                            "This can only be used if Bitcoin was enabled.");
                    }
                    else if (!configuratorSettings.LightningSettings.Implementation.Equals("lnd",
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        ModelState.AddModelError(
                            nameof(updateSettings.Settings) + "." +
                            nameof(updateSettings.Settings.ThunderHubSettings) + "." +
                            nameof(updateSettings.Settings.ThunderHubSettings.Enabled),
                            "ThunderHub currently only supports LND");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(updateSettings);
            }

            configuratorSettings.AdditionalServices = updateSettings.Settings;
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(AdvancedSettings));
        }
    }
}