using System;
using System.Text.Json;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("lightning")]
        public IActionResult LightningSettings()
        {
            var model = GetConfiguratorSettings();
            
            return View(new UpdateSettings<LightningSettings, AdditionalDataStub>()
            {
                Json = model.ToString(),
                Settings = model.LightningSettings,
            });
        }


        [HttpPost("lightning")]
        public IActionResult LightningSettings(
            UpdateSettings<LightningSettings, AdditionalDataStub> updateSettings)
        {
            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);
            if (configuratorSettings.ChainSettings.PruneMode != PruneMode.NoPruning &&
                updateSettings.Settings.Implementation == "eclair")
            {
                ModelState.AddModelError(
                    nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Implementation),
                    "You cannot use Eclair when you have pruning enabled.");
            }else if (configuratorSettings.ChainSettings.PruneMode == PruneMode.ExtraExtraSmall &&
                      !updateSettings.Settings.Implementation.Equals("none", StringComparison.InvariantCultureIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Implementation),
                    "You cannot use lightning when you have your level of pruning set.");
            }

            if (!ModelState.IsValid)
            {
                return View(updateSettings);
            }

            configuratorSettings.LightningSettings = updateSettings.Settings;
            if (configuratorSettings.LightningSettings.Implementation.Equals("eclair"))
            {
                configuratorSettings.AdvancedSettings.EnsureTxIndex();
            }
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(AdditionalServices));
        }
    }
}