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
        [HttpGet("advanced")]
        public IActionResult AdvancedSettings()
        {
            var model = GetConfiguratorSettings();
            return View(new UpdateSettings<AdvancedSettings, AdvancedSettingsAdditionalData>()
            {
                Json = model.ToString(),
                Settings = model.AdvancedSettings,
                Additional = new AdvancedSettingsAdditionalData()
                {
                    ShowSettings = model.AdvancedSettings.AnythingSet()
                }
            });
        }

        [HttpPost("advanced")]
        public IActionResult AdvancedSettings(UpdateSettings<AdvancedSettings, AdvancedSettingsAdditionalData> updateSettings,
            string command = null)
        {
            switch (command)
            {
                case { } x when x.StartsWith("fragmentset"):
                    var parts = x.Replace("fragmentset", "").Split(",");
                    foreach (var part in parts)
                    {
                        var add = part.StartsWith('+');
                        var fragment = part.TrimStart('+', '-');
                        if (add && !updateSettings.Settings.AdditionalFragments.Contains(fragment))
                        {
                            updateSettings.Settings.AdditionalFragments.Add(fragment);
                        }
                        else if (!add && updateSettings.Settings.AdditionalFragments.Contains(fragment))
                        {
                            updateSettings.Settings.AdditionalFragments.Remove(fragment);
                        }
                    }

                    updateSettings.Additional ??= new AdvancedSettingsAdditionalData();
                    updateSettings.Additional.ShowSettings = true;
                    return View(updateSettings);
                case "show":
                    updateSettings.Additional = new AdvancedSettingsAdditionalData()
                    {
                        ShowSettings = true
                    };
                    return View(updateSettings);
                case "add-additional":
                    updateSettings.Additional = new AdvancedSettingsAdditionalData()
                    {
                        ShowSettings = true
                    };
                    updateSettings.Settings.AdditionalFragments.Add("");
                    return View(updateSettings);
                case { } commandx
                    when commandx.StartsWith("remove-additional", StringComparison.InvariantCultureIgnoreCase):
                {
                    updateSettings.Additional = new AdvancedSettingsAdditionalData()
                    {
                        ShowSettings = true
                    };
                    var index = int.Parse(commandx.Substring(commandx.IndexOf(":", StringComparison.Ordinal) + 1));
                    updateSettings.Settings.AdditionalFragments.RemoveAt(index);
                    return View(updateSettings);
                }
                case "add-excluded":
                    updateSettings.Additional = new AdvancedSettingsAdditionalData()
                    {
                        ShowSettings = true
                    };
                    updateSettings.Settings.ExcludedFragments.Add("");
                    return View(updateSettings);
                case { } commandx
                    when commandx.StartsWith("remove-excluded", StringComparison.InvariantCultureIgnoreCase):
                {
                    updateSettings.Additional = new AdvancedSettingsAdditionalData()
                    {
                        ShowSettings = true
                    };
                    var index = int.Parse(commandx.Substring(commandx.IndexOf(":", StringComparison.Ordinal) + 1));
                    updateSettings.Settings.ExcludedFragments.RemoveAt(index);
                    return View(updateSettings);
                }
            }

            if (!ModelState.IsValid)
            {
                updateSettings.Additional = new AdvancedSettingsAdditionalData()
                {
                    ShowSettings = true
                };
                return View(updateSettings);
            }

            var configuratorSettings = string.IsNullOrEmpty(updateSettings.Json)
                ? new ConfiguratorSettings()
                : JsonSerializer.Deserialize<ConfiguratorSettings>(updateSettings.Json);
            configuratorSettings.AdvancedSettings = updateSettings.Settings;
            SetConfiguratorSettings(configuratorSettings);
            return RedirectToAction(nameof(Summary));
        }
    }
}