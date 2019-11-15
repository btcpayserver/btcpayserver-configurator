using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers
{
    public partial class ConfiguratorController
    {
        [HttpGet("summary")]
        public IActionResult Summary()
        {
            var model = GetConfiguratorSettings();
            return View(new UpdateSettings<ConfiguratorSettings, AdditionalDataStub>()
            {

                Settings = model
            });
        }
    }
}