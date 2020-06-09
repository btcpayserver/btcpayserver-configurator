using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BTCPayServerDockerConfigurator.Controllers
{
    [Route("")]
    public partial class ConfiguratorController : Controller
    {
        private readonly IOptions<ConfiguratorOptions> _options;
        private readonly ILogger<ConfiguratorController> _logger;
        private readonly DeploymentService _deploymentService;

        public bool IsVerified
        {
            
            get => HttpContext.Session.Keys.Contains("verified");
            set
            {
                if (HttpContext.Session.Keys.Contains("verified") && !value)
                {
                    TempData.Remove("verified");
                    return;
                }

                if (value)
                {
                    HttpContext.Session.SetString("verified", "verified");
                }
            }
        }

        public ConfiguratorController(IOptions<ConfiguratorOptions> options, ILogger<ConfiguratorController> logger,
            DeploymentService deploymentService)
        {
            _options = options;
            _logger = logger;
            _deploymentService = deploymentService;
        }

        private ConfiguratorSettings GetConfiguratorSettings()
        {
            return GetTempData<ConfiguratorSettings>(nameof(ConfiguratorSettings)) ?? new ConfiguratorSettings();
        }

        private void SetConfiguratorSettings(ConfiguratorSettings settings)
        {
            SetTempData(nameof(ConfiguratorSettings), settings);
        }

        private void SetTempData<T>(string name, T data)
        {
            if (TempData.ContainsKey(name))
                TempData.Remove(name);
            TempData.Add(name, JsonSerializer.Serialize(data));
        }

        private T GetTempData<T>(string name, bool remove = false) where T : class
        {
            if (remove && !TempData.ContainsKey(name))
            {
                return null;
            }

            var rawResult = remove ? TempData[name].ToString() : TempData.Peek(name)?.ToString();
            return string.IsNullOrEmpty(rawResult)
                ? null
                : JsonSerializer.Deserialize<T>(rawResult);
        }


        private string CheckHost(string host, ConfiguratorSettings configuratorSettings)
        {
            string hostToCheckAgainst = null;
            switch (configuratorSettings.DeploymentSettings.DeploymentType)
            {
                case DeploymentType.Manual:
                    break;
                case DeploymentType.ThisMachine:
                    hostToCheckAgainst = new WebClient().DownloadString("http://icanhazip.com").Trim()
                        .Replace(Environment.NewLine, "").Replace("\n", "");
                    break;
                case DeploymentType.RemoteMachine:
                    hostToCheckAgainst = configuratorSettings.DeploymentSettings.Host;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var errorMessage = string.IsNullOrEmpty(hostToCheckAgainst)
                ? "The domain was invalid."
                : $"The domain was either invalid or the DNS record did not seem to point to {hostToCheckAgainst}";

            if (!string.IsNullOrEmpty(host))
            {
                if (!CheckHostDNSIsCorrect(host, hostToCheckAgainst))
                {
                    return errorMessage;
                }
            }

            return null;
        }

        private bool CheckHostDNSIsCorrect(string host, string hostToCheckAgainst = null)
        {
            var basicCheck = Uri.CheckHostName(host);
            if (basicCheck != UriHostNameType.Dns && basicCheck != UriHostNameType.Unknown)
            {
                return false;
            }

            if (hostToCheckAgainst == null || host.EndsWith(".local", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            var vpsType = Uri.CheckHostName(hostToCheckAgainst);
            if (vpsType == UriHostNameType.Dns)
            {
                var vpsIps = Dns.GetHostAddresses(hostToCheckAgainst);
                return Dns.GetHostAddresses(host).ToList().Any(address =>
                    vpsIps.Any(ipAddress =>
                        ipAddress.Equals(address)));
            }

            return Dns.GetHostAddresses(host).ToList().Any(address =>
                address.ToString().Equals(hostToCheckAgainst, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}