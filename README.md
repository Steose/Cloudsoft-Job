# Cloudsoft Job

ASP.NET Core MVC job portal.

## Azure VM Deployment

### What `./infra/provision.sh` does

Running `./infra/provision.sh` now performs the full deployment flow:

1. Creates or updates the Azure resource group.
2. Deploys the Bicep infrastructure in `infra/main.bicep`.
3. Builds the Mongo connection string from the deployed Cosmos DB account.
4. Publishes `src/Cloudsoft.Web/Cloudsoft.Web.csproj`.
5. Waits for SSH on the bastion, proxy VM, and app VM.
6. Configures the proxy VM over SSH.
7. Configures the app VM over SSH.
8. Copies the published app to `/opt/cloudsoft` on the app VM.
9. Reloads `systemd` and starts Cloudsoft on the app VM.
10. Verifies the app locally on the app VM.
11. Verifies the reverse proxy publicly.

### Prerequisites

- Azure CLI installed and authenticated with `az login`
- Access to the subscription used by the deployment
- A private SSH key that matches the public key in `infra/main.bicepparam`
- By default the script expects the private key at `~/.ssh/id_ed25519`
- Python 3 available locally for URL-encoding the Cosmos DB credentials
- Ubuntu 22.04 app VMs install .NET 10 from the Ubuntu `dotnet/backports` feed if it is not already available in apt
- If apt still does not provide a working `dotnet` executable, the app VM setup falls back to the official `dotnet-install.sh` runtime installer

If your private key is somewhere else:

```bash
export SSH_PRIVATE_KEY_PATH=~/.ssh/your-key
```

## Run From The Repository Root

### Provision infrastructure and deploy the app:

```bash
./infra/provision.sh
```

The script prints the bastion IP, reverse proxy public URL, reverse proxy private IP, and app private IP after deployment.
It also disables strict SSH host key checking for its own automation so repeated redeployments do not fail on reused VM IPs.

### Production Configuration

The app now supports two production-safe configuration controls:

- `FeatureFlags:UseMongoDb`
  - when `false`, the app uses in-memory repositories
  - when `true`, the app only uses MongoDb if the Mongo settings are actually present
  - if the Mongo flag is on but configuration is missing, the app falls back to in-memory instead of crashing
  - if MongoDb is configured but runtime operations still fail, the repository layer now falls back to in-memory storage and logs the error
- `FeatureFlags:UseAzureKeyVault`
  - when `true`, the app tries to load configuration from Azure Key Vault before wiring repositories
  - if Key Vault is enabled but unavailable, the app logs the problem and continues without it

Key Vault settings:

```json
"KeyVault": {
  "VaultUri": "https://<your-vault-name>.vault.azure.net/",
  "ManagedIdentityClientId": ""
}
```

Key Vault secret names should use `--` for hierarchy. For example:

- `MongoDb--ConnectionString`
- `MongoDb--DatabaseName`
- `MongoDb--JobPostingsCollectionName`
- `MongoDb--EmployersCollectionName`

For Azure-hosted production, prefer:

- `FeatureFlags__UseMongoDb=true`
- `FeatureFlags__UseAzureKeyVault=true`
- `KeyVault__VaultUri=https://<your-vault-name>.vault.azure.net/`

The app uses `DefaultAzureCredential`, so a managed identity is the preferred production authentication path.

### Manual Recovery Or Verification

If you need to inspect the app VM after deployment, use the bastion host as a jump host:

```bash
ssh -i ~/.ssh/id_ed25519 -J azureuser@<bastion-public-ip> azureuser@<app-private-ip> '/opt/cloudsoft/verify-cloudsoft.sh'
```

Check the reverse proxy manually:

```bash
curl http://<reverse-proxy-public-ip>
```

If Nginx returns `502 Bad Gateway`, test the upstream from the proxy VM:

```bash
ssh -i ~/.ssh/id_ed25519 -J azureuser@<bastion-public-ip> azureuser@<reverse-proxy-private-ip> 'curl -i --max-time 10 http://10.0.2.4:8080 && sudo systemctl status nginx --no-pager'
```

### Why Existing Resource Groups Can Be Reused Now

The VM configuration is no longer pushed through Azure VM `customData`.
That avoids the Azure error:

```text
PropertyChangeNotAllowed: Changing property 'osProfile.customData' is not allowed.
```

Instead, `./infra/provision.sh` applies the proxy and app VM configuration over SSH every time it runs.

### VM Configuration Files

The repo includes:

- [setup-app-vm.sh](/Users/stephenucheosedumme/Cloudsoft-job/infra/setup-app-vm.sh)
- [setup-proxy-vm.sh](/Users/stephenucheosedumme/Cloudsoft-job/infra/setup-proxy-vm.sh)

These are the scripts `./infra/provision.sh` copies and executes remotely.

### App VM Configuration Reference

The app VM configuration template in [cloud-init-dotnet-app.yaml](/Users/stephenucheosedumme/Cloudsoft-job/infra/cloud-init-dotnet-app.yaml) mirrors the same setup applied by `setup-app-vm.sh`:

- installs `aspnetcore-runtime-10.0`
- creates `/opt/cloudsoft`
- writes `cloudsoft-web.service`
- writes `cloudsoft-web.path`
- auto-starts the web app once `/opt/cloudsoft/Cloudsoft.Web.dll` exists



## Use these exact commands from the repo root.

### Publish the updated app:

```bash
dotnet publish src/Cloudsoft.Web/Cloudsoft.Web.csproj -c Release -o ./publish
```

### Copy the published files to the app VM through the bastion:

```bash
scp -i ~/.ssh/id_ed25519 \
  -o ProxyJump=azureuser@4.210.106.35 \
  -r ./publish/* \
  azureuser@10.0.2.4:/opt/cloudsoft/
```

### Restart the app service on the app VM:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@4.210.106.35 \
  azureuser@10.0.2.4 \
  'sudo systemctl daemon-reload && sudo systemctl restart cloudsoft-web.service && sudo systemctl status cloudsoft-web.service --no-pager'
```

### Check the app locally on the app VM:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@4.210.106.35 \
  azureuser@10.0.2.4 \
  'curl -i --max-time 10 http://localhost:8080'
```

### Check the reverse proxy upstream from the proxy VM:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@4.210.106.35 \
  azureuser@10.0.1.4 \
  'curl -i --max-time 10 http://10.0.2.4:8080 && sudo systemctl status nginx --no-pager'
```

```bash
Check the public site:
curl -i http://52.156.211.239
```

### If the app still shows the generic error page, run this and paste the output:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@4.210.106.35 \
  azureuser@10.0.2.4 \
  'sudo journalctl -u cloudsoft-web.service -n 100 --no-pager'
  ```


### If you are using the Cosmos DB account from this repo, get the key first:

```bash
az cosmosdb keys list \
  --resource-group CloudsoftJobRG \
  --name clsjobcosmosmongo12345 \
  --type keys \
  --query primaryMasterKey \
  --output tsv
```

### Then build the connection string with:

```text
username = clsjobcosmosmongo12345
password = <primaryMasterKey>
host = clsjobcosmosmongo12345.mongo.cosmos.azure.com
database = clsjobdb
```

### After setting it, verify it:

```bash
az keyvault secret show \
  --vault-name clsjobkv12345 \
  --name "MongoDb--ConnectionString" \
  --query value \
  --output tsv
```
