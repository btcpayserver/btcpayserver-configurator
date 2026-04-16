using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers;

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

        if (model.DeploymentSettings.DeploymentType == DeploymentType.Manual)
        {
            var id = Guid.NewGuid().ToString();
            var result = new UpdateSettings<ConfiguratorSettings, DeployAdditionalData>
            {
                Additional = new DeployAdditionalData
                {
                    Bash = model.ConstructBashFile(),
                    CloudInitScript = model.ConstructCloudInitScript(),
                    ExitStatus = 0
                },
                Json = model.ToString(),
                Settings = model
            };
            SetTempData(id, result);
            return RedirectToAction("DeployResult", new { id });
        }

        return RedirectToAction("DeployResult", new
        {
            id = _deploymentService.StartDeployment(model, IsVerified)
        });
    }

    [HttpGet("deploy-result/{id?}")]
    public IActionResult DeployResult(string id = "", string view = null, bool json = false)
    {
        var result = _deploymentService.GetDeploymentResult(id);
        result ??=
            GetTempData<UpdateSettings<ConfiguratorSettings, DeployAdditionalData>>(id);
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

        return View(view ?? "DeployResult", result);
    }
}

public class DeployAdditionalData
{
    public string Bash { get; set; }
    public int ExitStatus { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public bool InProgress { get; set; }
    public string CloudInitScript { get; set; }
}
