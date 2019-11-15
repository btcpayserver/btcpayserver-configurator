using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BTCPayServerDockerConfigurator.Models
{
    public class ConfiguratorSettings
    {
        public DeploymentSettings DeploymentSettings { get; set; } = new DeploymentSettings();
        public DomainSettings DomainSettings { get; set; } = new DomainSettings();
        public LightningSettings LightningSettings { get; set; } = new LightningSettings();
        public AdvancedSettings AdvancedSettings { get; set; } = new AdvancedSettings();
        public ChainSettings ChainSettings { get; set; } = new ChainSettings();
        public AdditionalServices AdditionalServices { get; set; } = new AdditionalServices();

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public string ConstructBashFile(string downloadLink)
        {
            var result = new StringBuilder();
            
            result.AppendLine(GetAbstractedPackageManager());
            result.AppendLine(InstallPackage("git wget"));
            DownloadFile(downloadLink);
            var gitRepo = string.IsNullOrEmpty(AdvancedSettings.BTCPayDockerRepository)
                ? "https://github.com/btcpayserver/btcpayserver-docker" : AdvancedSettings.BTCPayDockerRepository;
            
            var gitBranch = string.IsNullOrEmpty(AdvancedSettings.BTCPayDockerBranch)
                ? "master" : AdvancedSettings.BTCPayDockerBranch;

            result.AppendLine($"git clone -b {gitBranch} {gitRepo}");
            result.AppendLine($"cd btcpayserver-docker");
            if (!string.IsNullOrEmpty(AdvancedSettings.CustomBTCPayImage))
            {
                result.AppendLine($"export BTCPAY_IMAGE=\"{AdvancedSettings.CustomBTCPayImage}\"");
            }

            if (gitBranch != "master" || gitRepo != "https://github.com/btcpayserver/btcpayserver-docker")
            {
                result.AppendLine($"export BTCPAYGEN_DOCKER_IMAGE=\"btcpayserver/docker-compose-generator:local\"");   
            }

            var additionalFragments = AdvancedSettings.AdditionalFragments;
            var excludedFragments = AdvancedSettings.ExcludedFragments;
            
            result.AppendLine($"export BTCPAY_IMAGE=\"{AdvancedSettings.CustomBTCPayImage}\"");
            var domain = string.IsNullOrEmpty(DomainSettings.Domain) ? "btcpay.local" : DomainSettings.Domain;
            result.AppendLine($"export BTCPAY_HOST=\"{domain}\"");
            if (DomainSettings.AdditionalDomains.Any())
            {
                result.AppendLine($"export BTCPAY_ADDITIONAL_HOSTS=\"{string.Join(',', DomainSettings.AdditionalDomains)}\"");
                
            }
            result.AppendLine($"export NBITCOIN_NETWORK=\"{ChainSettings.Network.ToString().ToLower()}\"");
            result.AppendLine($"export LIGHTNING_ALIAS=\"{LightningSettings.Alias}\"");
            result.AppendLine($"export BTCPAYGEN_LIGHTNING=\"{LightningSettings.Implementation}\"");
            var index = 1;
            if (ChainSettings.Bitcoin)
            {
                result.AppendLine($"export BTCPAYGEN_CRYPTO1=\"btc\"");
                index++;
            }

            foreach (var chainSettingsAltChain in ChainSettings.AltChains)
            {
                result.AppendLine($"export BTCPAYGEN_CRYPTO{index}=\"{chainSettingsAltChain}\"");
                index++;
            }
            
            result.AppendLine($"export BTCPAY_ENABLE_SSH=true");

            if (AdditionalServices.LibrePatronSettings.Enabled)
            {
                additionalFragments.Add("opt-add-librepatron");
                result.AppendLine($"export LIBREPATRON_HOST=\"{AdditionalServices.LibrePatronSettings.Host}\"");
            }
            if (AdditionalServices.TorRelaySettings.Enabled)
            {
                additionalFragments.Add("opt-add-tor-relay");
                result.AppendLine($"export TOR_RELAY_NICKNAME=\"{AdditionalServices.TorRelaySettings.Nickname}\"");
                result.AppendLine($"export TOR_RELAY_EMAIL=\"{AdditionalServices.TorRelaySettings.Email}\"");
            }
            if (AdditionalServices.WooCommerceSettings.Enabled)
            {
                additionalFragments.Add("opt-add-woocommerce");
                result.AppendLine($"export WOOCOMMERCE_HOST=\"{AdditionalServices.WooCommerceSettings.Host}\"");
            }
            if (AdditionalServices.BTCTransmuterSettings.Enabled)
            {
                additionalFragments.Add("opt-add-btctransmuter");
                result.AppendLine($"export BTCTRANSMUTER_HOST=\"{AdditionalServices.BTCTransmuterSettings.Host}\"");
            }
            
            if (additionalFragments.Any())
            {
                result.AppendLine($"export BTCPAYGEN_ADDITIONAL_FRAGMENTS=\"{string.Join(';', additionalFragments)}\"");
            }
            if (excludedFragments.Any())
            {
                result.AppendLine($"export BTCPAYGEN_EXCLUDE_FRAGMENTS=\"{string.Join(';', excludedFragments)}\"");
            }

            result.AppendLine(". ./btcpay.setup -i");
            return result.ToString();
        }

        private string GetAbstractedPackageManager()
        {
            return $"#!/bin/bash{Environment.NewLine}declare -A osInfo;{Environment.NewLine}osInfo[/etc/debian_version]=\"apt-get install -y\"{Environment.NewLine}osInfo[/etc/alpine-release]=\"apk --update add\"{Environment.NewLine}osInfo[/etc/centos-release]=\"yum install -y\"{Environment.NewLine}osInfo[/etc/fedora-release]=\"dnf install -y\"{Environment.NewLine}{Environment.NewLine}for f in ${!osInfo[@]}{Environment.NewLine}do{Environment.NewLine}    if [[ -f $f ]];then{Environment.NewLine}        package_manager=${osInfo[$f]}{Environment.NewLine}    fi{Environment.NewLine}done";
        }

        private string InstallPackage(string package)
        {
            return "${package_manager} "+ package;
        }

        private string DownloadFile(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            return $"wget {url} 2>/dev/null || curl -O  {url}";
        }
        
    }
}