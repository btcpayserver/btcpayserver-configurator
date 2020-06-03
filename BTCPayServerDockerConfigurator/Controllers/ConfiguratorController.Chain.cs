using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("chain")]
        public IActionResult ChainSettings()
        {
            var model = GetConfiguratorSettings();
            return View(new UpdateSettings<ChainSettings, AdditionalDataStub>()
            {
                Json = model.ToString(),
                Settings = model.ChainSettings,
            });
        }

        [HttpPost("chain")]
        public IActionResult ChainSettings(UpdateSettings<ChainSettings, AdditionalDataStub> updateSettings,
            string command = null)
        {
            switch (command)
            {
                case "add-chain":
                    updateSettings.Settings.AltChains.Add("");
                    return View(updateSettings);
                case string commandx
                    when commandx.StartsWith("remove-chain", StringComparison.InvariantCultureIgnoreCase):
                {
                    var index = int.Parse(commandx.Substring(commandx.IndexOf(":", StringComparison.Ordinal) + 1));
                    updateSettings.Settings.AltChains.RemoveAt(index);
                    return View(updateSettings);
                }
            }

            updateSettings.Settings.AltChains =
                updateSettings.Settings.AltChains.Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (!updateSettings.Settings.Bitcoin && !updateSettings.Settings.AltChains.Any())
            {
                updateSettings.Settings.Bitcoin = true;
                // ModelState.AddModelError(
                //     nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Bitcoin),
                //     "You need to set up at least one chain");
            }

            if (!ModelState.IsValid)
            {
                return View(updateSettings);
            }

            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);
            configuratorSettings.ChainSettings = updateSettings.Settings;
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(LightningSettings));
        }
    }
}