using System.Text;
using System.Text.Json;

namespace BTCPayServerDockerConfigurator.Models;

public class ConfiguratorSettings
{
    public DeploymentSettings DeploymentSettings { get; set; } = new();
    public DomainSettings DomainSettings { get; set; } = new();
    public LightningSettings LightningSettings { get; set; } = new();
    public AdvancedSettings AdvancedSettings { get; set; } = new();
    public ChainSettings ChainSettings { get; set; } = new();
    public AdditionalServices AdditionalServices { get; set; } = new();
    public ServerData ServerData { get; set; } = new();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public string ConstructBashFile()
    {
        var result = new StringBuilder();
        if (string.IsNullOrEmpty(DeploymentSettings.RootPassword))
        {
            result.AppendLine("sudo su -");
        }
        else
        {
            result.AppendLine(
                $"echo \"{DeploymentSettings.RootPassword}\" | sudo -S sleep 1 && sudo su -");
        }

        result.AppendLine(InstallPackage("git wget"));
        var gitRepo = string.IsNullOrEmpty(AdvancedSettings.BTCPayDockerRepository)
            ? "https://github.com/btcpayserver/btcpayserver-docker"
            : AdvancedSettings.BTCPayDockerRepository;

        var gitBranch = string.IsNullOrEmpty(AdvancedSettings.BTCPayDockerBranch)
            ? "master"
            : AdvancedSettings.BTCPayDockerBranch;

        result.AppendLine("cd ~");
        result.AppendLine(
            $"EXISTING_BRANCH=\"$(if [ -d 'btcpayserver-docker' ]; then git -C 'btcpayserver-docker' branch --show-current; fi)\"");
        result.AppendLine(
            $"EXISTING_REMOTE=\"$(if [ -d 'btcpayserver-docker' ]; then git -C 'btcpayserver-docker' config --get remote.origin.url; fi)\"");

        result.AppendLine(
            $"if [ -d \"btcpayserver-docker\" ] && {{ [ \"$EXISTING_REMOTE\" != \"{gitRepo}\" ] || [ \"$EXISTING_BRANCH\" != \"{gitBranch}\" ]; }}; then echo \"Existing btcpayserver-docker folder found with different fork/branch. Backing up.\"; mv \"btcpayserver-docker\" \"btcpayserver-docker_$(date +%s)\"; fi");

        result.AppendLine(
            $"if [ -d \"btcpayserver-docker\" ]; then echo \"Existing btcpayserver-docker folder found, pulling latest changes.\"; git -C \"btcpayserver-docker\" pull; fi");
        result.AppendLine(
            $"if [ ! -d \"btcpayserver-docker\" ]; then echo \"Cloning btcpayserver-docker\"; git clone -b {gitBranch} {gitRepo} btcpayserver-docker; fi");

        if (!string.IsNullOrEmpty(AdvancedSettings.CustomBTCPayImage))
        {
            result.AppendLine($"export BTCPAY_IMAGE=\"{AdvancedSettings.CustomBTCPayImage}\"");
        }

        if (gitBranch != "master" ||
            gitRepo != "https://github.com/btcpayserver/btcpayserver-docker")
        {
            result.AppendLine(
                "export BTCPAYGEN_DOCKER_IMAGE=\"btcpayserver/docker-compose-generator:local\"");
        }

        var additionalFragments = AdvancedSettings.AdditionalFragments;
        var excludedFragments = AdvancedSettings.ExcludedFragments;
        var domain = string.IsNullOrEmpty(DomainSettings.Domain)
            ? "btcpay.local"
            : DomainSettings.Domain;
        result.AppendLine($"export BTCPAY_HOST=\"{domain}\"");
        if (DomainSettings.AdditionalDomains.Any())
        {
            result.AppendLine(
                $"export BTCPAY_ADDITIONAL_HOSTS=\"{string.Join(',', DomainSettings.AdditionalDomains)}\"");
        }

        result.AppendLine(
            $"export NBITCOIN_NETWORK=\"{ChainSettings.Network.ToString().ToLower()}\"");
        result.AppendLine($"export LIGHTNING_ALIAS=\"{LightningSettings.Alias}\"");
        result.AppendLine(
            $"export BTCPAYGEN_LIGHTNING=\"{(LightningSettings.Implementation == "none" ? string.Empty : LightningSettings.Implementation)}\"");
        var index = 1;
        if (ChainSettings.Bitcoin)
        {
            result.AppendLine("export BTCPAYGEN_CRYPTO1=\"btc\"");
            index++;
        }

        foreach (var chainSettingsAltChain in ChainSettings.AltChains)
        {
            result.AppendLine($"export BTCPAYGEN_CRYPTO{index}=\"{chainSettingsAltChain}\"");
            index++;
        }

        switch (ChainSettings.PruneMode)
        {
            case PruneMode.NoPruning:
                break;
            case PruneMode.Minimal:
                additionalFragments.Add("opt-save-storage");
                break;
            case PruneMode.Small:
                additionalFragments.Add("opt-save-storage-s");
                break;
            case PruneMode.ExtraSmall:
                additionalFragments.Add("opt-save-storage-xs");
                break;
            case PruneMode.ExtraExtraSmall:
                additionalFragments.Add("opt-save-storage-xxs");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        result.AppendLine("export BTCPAY_ENABLE_SSH=true");

        foreach (var svc in ServiceRegistry.All)
        {
            svc.ApplyToScript(result, additionalFragments, AdditionalServices);
        }

        if (additionalFragments.Any())
        {
            result.AppendLine(
                $"export BTCPAYGEN_ADDITIONAL_FRAGMENTS=\"{string.Join(';', additionalFragments)}\"");
        }

        if (excludedFragments.Any())
        {
            result.AppendLine(
                $"export BTCPAYGEN_EXCLUDE_FRAGMENTS=\"{string.Join(';', excludedFragments)}\"");
        }

        result.AppendLine("cd btcpayserver-docker");
        result.AppendLine(". ./btcpay-setup.sh -i");

        if (ChainSettings.FastSync && ChainSettings.Bitcoin &&
            ChainSettings.PruneMode != PruneMode.NoPruning)
        {
            result.AppendLine("cd contrib/FastSync");
            result.AppendLine("./load-utxo-set.sh");
        }

        return result.ToString();
    }

    public string ConstructCloudInitScript()
    {
        var result = new StringBuilder();
        result.AppendLine("#cloud-config");
        result.AppendLine("runcmd:");
        result.AppendLine("  - |");
        var bashLines = ConstructBashFile()
            .Replace(Environment.NewLine, "\n")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in bashLines)
        {
            // Skip sudo su lines — cloud-init already runs as root
            if (line.TrimStart().StartsWith("sudo su") ||
                line.Contains("sudo -S sleep 1 && sudo su"))
                continue;
            result.AppendLine($"    {line}");
        }

        return result.ToString();
    }

    private string InstallPackage(string package)
    {
        return "apt-get update && apt-get install -y " + package;
    }

    public SSHSettings GetSshSettings(ConfiguratorOptions configuratorOptions, bool verified)
    {
        SSHSettings ssh = null;
        switch (DeploymentSettings.DeploymentType)
        {
            case DeploymentType.RemoteMachine:
            {
                ssh = new SSHSettings
                {
                    Server = DeploymentSettings.Host,
                    Username = DeploymentSettings.Username,
                    RootPassword = DeploymentSettings.RootPassword
                };
                if (DeploymentSettings.AuthMethod == SSHAuthMethod.SSHKey &&
                    !string.IsNullOrEmpty(DeploymentSettings.SSHPrivateKey))
                {
                    ssh.PrivateKeyContent = DeploymentSettings.SSHPrivateKey;
                }
                else
                {
                    ssh.Password = DeploymentSettings.Password;
                }

                break;
            }
            case DeploymentType.ThisMachine:
            {
                if (verified)
                {
                    ssh = configuratorOptions.ParseSSHConfiguration();
                }

                break;
            }
        }

        if (ssh == null)
        {
            throw new InvalidOperationException(
                "Cannot create SSH settings for this deployment type");
        }

        return ssh;
    }
}
