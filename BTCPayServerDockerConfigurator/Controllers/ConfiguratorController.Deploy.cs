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
            var ssh = GetConfiguratorSettings().GetSshSettings(_options.Value, IsVerified);
            using var sshC = await ssh.ConnectAsync();
            var result =
                await sshC.RunBash(
                    "cat /var/lib/docker/volumes/generated_tor_servicesdir/_data/BTCPayServer/hostname");
            await sshC.DisconnectAsync();
            return View(result);
        }

        [HttpPost("deploy")]
        public IActionResult Deploy()
        {
            var model = GetConfiguratorSettings();
            var id = _deploymentService.StartDeployment(model, IsVerified);
            return RedirectToAction("DeployResult", new
            {
                id = id
            });

        }
        

        [HttpGet("deploy-result/{id?}")]
        public IActionResult DeployResult(string id ="", string view = null, bool json = false)
        {
            var result = _deploymentService.GetDeploymentResult(id);
            if (result == null)
            {
                result = GetTempData<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>(id);
            }
            if (result == null)
            {
                return RedirectToAction("Summary");
            }

            if (!result.Additional.InProgress)
            {
                SetTempData(id, result);
            }

            if (json)
            {
                return Json(result);
            }
            return View(view?? "DeployResult",result);
        }
    }

    public class DeployAdditionalData
    {
        public string Bash { get; set; }
        public int ExitStatus { get;set; }
        public string Output { get; set;  }
        public string Error { get; set;  }
        public bool InProgress { get; set; } = false;
    }
}