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
        [HttpGet("domain")]
        public IActionResult DomainSettings()
        {
            var model = GetConfiguratorSettings() ?? new ConfiguratorSettings();

            return View(new UpdateSettings<DomainSettings, AdditionalDataStub>()
            {
                Json = JsonSerializer.Serialize(model),
                Settings = model.DomainSettings,
            });
        }

        [HttpPost("domain")]
        public IActionResult DomainSettings(
            UpdateSettings<DomainSettings, AdditionalDataStub> updateSettings,
            string command = null)
        {
            switch (command)
            {
                case "add-domain":
                    if (!string.IsNullOrEmpty(updateSettings.Settings.Domain))
                    {
                        updateSettings.Settings.AdditionalDomains.Add("");
                    }

                    return View(updateSettings);
                case string commandx
                    when commandx.StartsWith("remove-domain", StringComparison.InvariantCultureIgnoreCase):
                {
                    var index = int.Parse(commandx.Substring(commandx.IndexOf(":", StringComparison.Ordinal) + 1));
                    updateSettings.Settings.AdditionalDomains.RemoveAt(index);
                    return View(updateSettings);
                }
            }

            if (updateSettings.Settings.AdditionalDomains.Any() &&
                string.IsNullOrEmpty(updateSettings.Settings.Domain))
            {
                ModelState.AddModelError(nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Domain),
                    "You cannot set additional domains when there is no primary domain set!");
            }

            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);

            var error = CheckHost(updateSettings.Settings.Domain, configuratorSettings);
            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError(nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Domain),
                    error);
            }

            error = null;
            for (var index = 0; index < updateSettings.Settings.AdditionalDomains.Count; index++)
            {
                var additionalDomain = updateSettings.Settings.AdditionalDomains[index];
                error = CheckHost(additionalDomain, configuratorSettings);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError(
                        nameof(updateSettings.Settings) + "." + nameof(updateSettings.Settings.Domain) + $"[{index}]",
                        error);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(updateSettings);
            }

            configuratorSettings.DomainSettings = updateSettings.Settings;
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(ChainSettings));
        }
       
    }
}