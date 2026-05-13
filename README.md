# Cloudsoft Job

Cloudsoft Job is an ASP.NET Core job portal for browsing jobs and allowing employers to register, sign in, and post job listings.

The solution includes:

- `Cloudsoft.Web`: MVC web application.
- `Cloudsoft.Api`: API project.
- `Cloudsoft.Core`: shared models, services, repositories, and storage helpers.
- `test/Cloudsoft.Tests`: xUnit tests.
- `docker/`: local container setup.
- `infra/vm-deploy/`: Azure VM deployment.
- `infra/container-apps/`: Azure Container Apps deployment.
- `.github/workflows/ci-cd.yml`: GitHub Actions build, test, and deploy workflow.

## Prerequisites

Install these tools for local development:

- .NET SDK 10
- Docker Desktop, if using Docker Compose
- Azure CLI, if deploying to Azure
- Git

Check .NET:

```bash
dotnet --info
```

Check Azure login:

```bash
az account show
```

If Azure CLI is not logged in:

```bash
az login
```

## Project Structure

```text
Cloudsoft-job/
├── src/
│   ├── Cloudsoft.Web/
│   ├── Cloudsoft.Api/
│   └── Cloudsoft.Core/
├── test/
│   └── Cloudsoft.Tests/
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── infra/
│   ├── vm-deploy/
│   └── container-apps/
├── docs/
└── .github/workflows/ci-cd.yml
```

## Local Setup With .NET

Use this when you want to run the app directly on your machine.

### 1. Restore Packages

From the repository root:

```bash
dotnet restore Cloudsoft.sln
```

### 2. Build The Solution

```bash
dotnet build Cloudsoft.sln --no-restore
```

### 3. Run Tests

```bash
dotnet test Cloudsoft.sln --no-build --no-restore
```

### 4. Run The Web App

```bash
dotnet run --project src/Cloudsoft.Web/Cloudsoft.Web.csproj
```

The local app normally runs at:

```text
http://localhost:5154
```

Important pages:

```text
http://localhost:5154/
http://localhost:5154/Home/About
http://localhost:5154/Job
http://localhost:5154/Account/Login
http://localhost:5154/Account/Register
```

## Local Configuration

Local settings are in:

```text
src/Cloudsoft.Web/appsettings.Development.json
```

Important local flags:

```json
"FeatureFlags": {
  "UseMongoDb": true,
  "UseAzureKeyVault": false,
  "UseAzureStorage": false
}
```

Meaning:

- `UseMongoDb=true`: the app tries MongoDB first.
- `UseAzureKeyVault=false`: local development does not load secrets from Azure Key Vault.
- `UseAzureStorage=false`: local development uses `wwwroot/images/hero.png`.

The local MongoDB connection is:

```json
"MongoDb": {
  "ConnectionString": "mongodb://localhost:27017/?serverSelectionTimeoutMS=2000&connectTimeoutMS=2000",
  "DatabaseName": "Cloudsoft"
}
```

If MongoDB is not running, the app falls back to in-memory storage.

## Local Setup With Docker Compose

Use this when you want the app, MongoDB, and Mongo Express to run in containers.

### 1. Start Containers

```bash
docker compose -f docker/docker-compose.yml up --build
```

### 2. Open The App

```text
http://localhost:8080
```

### 3. Open Mongo Express

```text
http://localhost:8081
```

### 4. Stop Containers

```bash
docker compose -f docker/docker-compose.yml down
```

## User Secrets For Local MongoDB

Use user secrets if you want to keep a real MongoDB connection string out of source code.

Run this from the web project folder:

```bash
cd src/Cloudsoft.Web
dotnet user-secrets set "MongoDb:ConnectionString" "<your-mongodb-connection-string>"
```

Then run the app:

```bash
dotnet run
```

## Azure Production Option 1: VM Deployment

Use this option when deploying to Azure VMs with:

- Bastion VM for SSH access
- Proxy VM with Nginx
- Private App VM running ASP.NET Core
- Cosmos DB Mongo
- Azure Blob Storage for `hero.png`

The VM deployment files are in:

```text
infra/vm-deploy/
```

### VM Deployment Files

- `main.bicep`: creates the Azure infrastructure.
- `main.bicepparam`: provides deployment parameter values.
- `provision.sh`: full deployment script.
- `setup-proxy-vm.sh`: installs and configures Nginx.
- `setup-app-vm.sh`: installs .NET, writes environment variables, creates systemd service, and starts the app.

### VM Deployment Prerequisites

You need:

- Azure CLI logged in.
- Permission to create Azure resources.
- SSH private key matching the public key in `infra/vm-deploy/main.bicepparam`.
- .NET SDK 10 installed locally.
- Python 3 available locally.

By default, the script expects:

```text
~/.ssh/id_ed25519
```

If your key is somewhere else:

```bash
export SSH_PRIVATE_KEY_PATH=~/.ssh/your-key
```

### Run Full VM Deployment

From the repository root:

```bash
cd infra/vm-deploy
./provision.sh
```

The script does the full flow:

1. Creates or updates the Azure resource group.
2. Deploys `main.bicep`.
3. Reads Azure deployment outputs.
4. Builds the Cosmos MongoDB connection string.
5. Uploads `src/Cloudsoft.Web/wwwroot/images/hero.png` to Azure Blob Storage.
6. Publishes `Cloudsoft.Web`.
7. Connects to the private VMs through the Bastion VM.
8. Configures the Proxy VM with Nginx.
9. Configures the App VM with .NET, environment variables, and systemd.
10. Copies the published app to `/opt/cloudsoft`.
11. Restarts the `cloudsoft-web` service.
12. Verifies the app and public reverse proxy.

### VM Production Request Flow

```text
Browser
-> Proxy VM public IP
-> Nginx on Proxy VM
-> App VM private IP on port 8080
-> Cloudsoft.Web
-> Cosmos DB Mongo and Azure Blob Storage
```

### Verify VM Production

The script prints the reverse proxy URL at the end.

Open:

```text
http://<reverse-proxy-public-ip>
http://<reverse-proxy-public-ip>/Home/About
http://<reverse-proxy-public-ip>/Job
```

Check the app VM through the bastion:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@<bastion-public-ip> \
  azureuser@<app-private-ip> \
  '/opt/cloudsoft/verify-cloudsoft.sh'
```

Check app logs:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@<bastion-public-ip> \
  azureuser@<app-private-ip> \
  'sudo journalctl -u cloudsoft-web -n 100 --no-pager'
```

Check proxy VM:

```bash
ssh -i ~/.ssh/id_ed25519 \
  -J azureuser@<bastion-public-ip> \
  azureuser@<proxy-private-ip> \
  'curl -i --max-time 10 http://<app-private-ip>:8080 && sudo systemctl status nginx --no-pager'
```

## Azure Production Option 2: Container Apps With GitHub Actions

Use this option when deploying through CI/CD to Azure Container Apps.

The workflow is:

```text
.github/workflows/ci-cd.yml
```

The Azure Container Apps Bicep files are:

```text
infra/container-apps/registry.bicep
infra/container-apps/main.bicep
```

### CI/CD Flow

On pull requests:

1. Checkout code.
2. Install .NET 10.
3. Restore packages.
4. Build the solution.
5. Run tests.

On push to `main`:

1. Run build and tests.
2. Log in to Azure using OIDC.
3. Create or reuse the Azure resource group.
4. Create or update Azure Container Registry.
5. Build the Docker image.
6. Push the Docker image to ACR.
7. Deploy Azure Container Apps with Bicep.
8. Print the deployed Container App URL.

### Required GitHub Secrets

Set these in:

```text
GitHub repository -> Settings -> Secrets and variables -> Actions -> Secrets
```

Required secrets:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
MONGODB_CONNECTION_STRING
AZURE_BLOB_CONTAINER_URL
KEY_VAULT_URI
```

For the current OIDC setup, do not use `AZURE_CLIENT_SECRET`.

### Required Azure Federated Credential

The GitHub Actions Azure app registration needs a federated credential for the production environment:

```text
repo:<owner>/<repo>:environment:production
```

For this repository, the subject is:

```text
repo:Steose/Cloudsoft-Job:environment:production
```

The audience must be:

```text
api://AzureADTokenExchange
```

### Required Azure Roles

The GitHub Actions service principal needs:

```text
Contributor
User Access Administrator
```

Scope:

```text
/subscriptions/<subscription-id>/resourceGroups/cloudsoft-job-aca-rg
```

`User Access Administrator` is needed because `main.bicep` creates an `AcrPull` role assignment for the Container App managed identity.

### Optional GitHub Variables

Set these in:

```text
GitHub repository -> Settings -> Secrets and variables -> Actions -> Variables
```

Optional variables:

```text
AZURE_LOCATION
AZURE_RESOURCE_GROUP
ACA_PREFIX
IMAGE_REPOSITORY
USE_MONGODB
USE_AZURE_STORAGE
USE_AZURE_KEY_VAULT
```

Default values are defined in `.github/workflows/ci-cd.yml`.

### Trigger Production Deployment

Commit and push to `main`:

```bash
git push origin main
```

Then open:

```text
GitHub repository -> Actions -> CI/CD
```

## Azure Blob Image Setup

The app uses `hero.png` as the background image.

Local image path:

```text
src/Cloudsoft.Web/wwwroot/images/hero.png
```

VM deployment uploads it to:

```text
https://<storage-account>.blob.core.windows.net/images/hero.png
```

The web app uses:

```text
AzureBlob__ContainerUrl=https://<storage-account>.blob.core.windows.net/images
FeatureFlags__UseAzureStorage=true
```

If the image does not display:

1. Open the Blob URL directly in the browser.
2. Confirm the container public access is `Blob`.
3. Confirm the app rendered URL does not contain `images/images/hero.png`.
4. Redeploy the app if the VM is running old code.

## Common Commands

Build:

```bash
dotnet build Cloudsoft.sln
```

Test:

```bash
dotnet test Cloudsoft.sln
```

Run web app:

```bash
dotnet run --project src/Cloudsoft.Web/Cloudsoft.Web.csproj
```

Publish web app:

```bash
dotnet publish src/Cloudsoft.Web/Cloudsoft.Web.csproj -c Release -o ./publish
```

Run Docker Compose:

```bash
docker compose -f docker/docker-compose.yml up --build
```

Stop Docker Compose:

```bash
docker compose -f docker/docker-compose.yml down
```

Run VM deployment:

```bash
cd infra/vm-deploy
./provision.sh
```

## Troubleshooting

### `dotnet run` Works But `/Job` Is Slow

The app may be trying MongoDB first. In development, MongoDB has a short timeout and then falls back to in-memory storage.

Start MongoDB locally or use Docker Compose:

```bash
docker compose -f docker/docker-compose.yml up --build
```

### `hero.png` Does Not Display In Production

Check the rendered About page HTML and look at the background image URL.

It should be:

```text
https://<storage-account>.blob.core.windows.net/images/hero.png
```

It should not be:

```text
https://<storage-account>.blob.core.windows.net/images/images/hero.png
```

If the deployed app still has the wrong URL, publish and redeploy the latest app.

### Azure Login Fails In GitHub Actions

Check:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- Federated credential subject

More details:

```text
docs/ci-cd-workflow-explanation.md
```

### Azure DeploymentActive Error

Cancel the stuck deployment:

```bash
az deployment group cancel \
  --resource-group cloudsoft-job-aca-rg \
  --name main
```

Then re-run the workflow.

### Azure Role Assignment Error

If Bicep fails on:

```text
Microsoft.Authorization/roleAssignments/write
```

The GitHub Actions service principal needs:

```text
User Access Administrator
```

More details:

```text
docs/ci-cd-workflow-explanation.md
```

## Documentation

More documentation is available in:

- `docs/ci-cd-workflow-explanation.md`
- `docs/azure-container-apps-deployment.md`
- `docs/azure-container-registry-image-push.md`
- `docs/github-actions-main-deploy.md`
- `docs/azure-storage-images.md`
- `infra/vm-deploy/README.md`
- `test/README.md`

