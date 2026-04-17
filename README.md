# BTCPay Server Configurator

A web-based wizard for configuring and deploying [BTCPay Server](https://btcpayserver.org) via Docker. Deploy directly to a remote server over SSH or generate a bash script for manual deployment.

## Use Cases

- **Self-hosting migration** — Third-party hosts can offer a Configurator instance so users can easily transition to their own server.
- **Consulting / managed deployments** — Server admins deploying BTCPay on behalf of clients can export or directly deploy configurations.
- **Existing server reconfiguration** — Admins can modify a running BTCPay Server's settings from the Configurator (admin account required).

## Setup

### Option 1: Add as an external service to BTCPay

If you already have a BTCPay Server [deployed](https://docs.btcpayserver.org/Deployment/) with the `opt-add-configurator` [environment variable](https://docs.btcpayserver.org/FAQ/FAQ-Deployment#how-can-i-modify-or-deactivate-environment-variables):

**Server Settings > Services > Other external services > Configurator > See information**

Non-admins can access the Configurator at `yourbtcpaydomain.com/configurator`.

### Option 2: Run with Docker

```bash
docker run -p 1337:80 ghcr.io/btcpayserver/btcpayserver-configurator
```

Then open **http://localhost:1337** in your browser.

### Option 3: Build from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/btcpayserver/btcpayserver-configurator.git
cd btcpayserver-configurator
dotnet run --project BTCPayServerDockerConfigurator
```

## Wizard Steps

The configurator walks through 6 steps:

1. **Destination** — Deploy via SSH now, or generate a bash script for later.
2. **Domain** — Set the domain name pointing to your server.
3. **Chain** — Choose Bitcoin network (mainnet/testnet), pruning level, and optional altcoins.
4. **Lightning** — Pick a Lightning implementation: LND, Core Lightning, Eclair, or Phoenixd (optional).
5. **Advanced** — Additional settings like reverse proxy, custom BTCPAY_IMAGE, startup options.
6. **Summary** — Review all selections, then deploy or export the script.

### SSH Deployment

When deploying over SSH, the Configurator automates:

- Installing Docker, Docker Compose, and Git
- Configuring BTCPay environment variables
- Setting up systemd for auto-start on reboot
- Adding BTCPay utilities to `/usr/bin`
- Starting BTCPay Server

### Manual Script Export

Choose "Generate Script" in step 1 to get a bash script you can paste into your server terminal later.

## Docker Images

Images are published to [GitHub Container Registry](https://ghcr.io/btcpayserver/btcpayserver-configurator) on every push to `master`.

| Tag | Description |
|-----|-------------|
| `latest` | Every master push |
| `x.y.z` | Versioned release (e.g. `0.0.23`) |
| `x.y` | Major.minor (e.g. `0.0`) |

Versioning is driven by the `<Version>` property in `BTCPayServerDockerConfigurator.csproj`. When the version is bumped, CI automatically creates a git tag and publishes versioned Docker images.

## Privacy & Security

If using someone else's Configurator instance, you will be sharing:

- Your server IP/domain and SSH credentials
- Your server configuration choices

**Mitigations:**
- Change your SSH password after deployment.
- Use [local Docker deployment](#option-2-run-with-docker) or [export the script](#manual-script-export) without entering your domain — add it when you paste the commands into your terminal.
