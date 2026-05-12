# CI/CD Workflow Explanation

This file explains `.github/workflows/ci-cd.yml` step by step.

## What The Workflow Does

The workflow has two main parts:

1. Build and test the application.
2. Deploy the app to Azure Container Apps when code is pushed to `main`.

## When It Runs

```yaml
on:
  push:
    branches:
      - main
  pull_request:
```

The workflow runs in two cases:

- When a pull request is opened or updated.
- When code is pushed to the `main` branch.

Pull requests only run build and tests. Deployment only runs after a push to `main`.

## Permissions

```yaml
permissions:
  contents: read
  id-token: write
```

`contents: read` allows GitHub Actions to read the repository code.

`id-token: write` allows GitHub Actions to log in to Azure using federated identity instead of storing an Azure password.

## Environment Variables

```yaml
env:
  DOTNET_VERSION: '10.0.x'
  AZURE_LOCATION: ${{ vars.AZURE_LOCATION || 'northeurope' }}
  AZURE_RESOURCE_GROUP: ${{ vars.AZURE_RESOURCE_GROUP || 'cloudsoft-job-aca-rg' }}
  ACA_PREFIX: ${{ vars.ACA_PREFIX || 'cloudsoftjob' }}
  IMAGE_REPOSITORY: ${{ vars.IMAGE_REPOSITORY || 'cloudsoft-job' }}
```

These values are used by the whole workflow.

- `DOTNET_VERSION` chooses the .NET SDK version.
- `AZURE_LOCATION` chooses the Azure region.
- `AZURE_RESOURCE_GROUP` is the resource group for Azure Container Apps.
- `ACA_PREFIX` is used when naming Azure resources.
- `IMAGE_REPOSITORY` is the Docker image repository name in Azure Container Registry.

If GitHub repository variables are not set, the workflow uses the default values.

## Job 1: Build And Test

```yaml
build-test:
  runs-on: ubuntu-latest
```

This job runs on a Linux GitHub Actions runner.

### Step 1: Checkout Code

```yaml
- uses: actions/checkout@v4
```

This downloads the repository code into the GitHub Actions runner.

### Step 2: Install .NET

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}
```

This installs the .NET SDK version defined by `DOTNET_VERSION`.

### Step 3: Restore Packages

```yaml
- run: dotnet restore Cloudsoft.sln
```

This downloads all NuGet packages needed by the solution.

### Step 4: Build Solution

```yaml
- run: dotnet build Cloudsoft.sln --no-restore
```

This builds all projects in `Cloudsoft.sln`.

`--no-restore` is used because packages were already restored in the previous step.

### Step 5: Run Tests

```yaml
- run: dotnet test Cloudsoft.sln --no-build --no-restore
```

This runs all tests in the solution.

`--no-build` and `--no-restore` are used because the solution was already restored and built.

## Job 2: Deploy To Azure Container Apps

```yaml
deploy-azure-container-apps:
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'
  needs: build-test
```

This job only runs when code is pushed to `main`.

It also depends on `build-test`, so deployment only starts if build and tests pass.

## Deploy Step 1: Checkout Code

```yaml
- uses: actions/checkout@v4
```

This downloads the repository again for the deployment job.

Each job starts with a clean runner, so checkout is needed again.

## Deploy Step 2: Azure Login

```yaml
- name: Azure login
  uses: azure/login@v2
```

This logs GitHub Actions into Azure.

It uses these GitHub secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

These values identify the Azure app registration or managed identity used by GitHub Actions.

## Deploy Step 3: Create Resource Group

```yaml
az group create \
  --name "$AZURE_RESOURCE_GROUP" \
  --location "$AZURE_LOCATION"
```

This creates the Azure resource group if it does not already exist.

If the resource group already exists, Azure keeps it and returns the existing resource group.

## Deploy Step 4: Create Or Update Azure Container Registry

```yaml
az deployment group create \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --template-file infra/container-apps/registry.bicep
```

This deploys `infra/container-apps/registry.bicep`.

That Bicep file creates or updates Azure Container Registry.

After deployment, the workflow reads two outputs:

- `acrName`
- `acrLoginServer`

These outputs are saved into GitHub Actions output variables so later steps can use them.

## Deploy Step 5: Build And Push Docker Image

```yaml
docker build \
  --file docker/Dockerfile \
  --tag "$ACR_LOGIN_SERVER/$IMAGE_REPOSITORY:$IMAGE_TAG" \
  --tag "$ACR_LOGIN_SERVER/$IMAGE_REPOSITORY:latest" \
  .
```

This builds the Docker image for the app.

The image gets two tags:

- The commit SHA tag, for example `abc123`.
- The `latest` tag.

Then both tags are pushed to Azure Container Registry:

```yaml
docker push "$ACR_LOGIN_SERVER/$IMAGE_REPOSITORY:$IMAGE_TAG"
docker push "$ACR_LOGIN_SERVER/$IMAGE_REPOSITORY:latest"
```

## Deploy Step 6: Deploy Azure Container App

```yaml
az deployment group create \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --template-file infra/container-apps/main.bicep
```

This deploys the Azure Container Apps infrastructure using `infra/container-apps/main.bicep`.

It passes important values into Bicep:

- Azure region
- Resource prefix
- Docker image repository
- Docker image tag
- MongoDB setting
- Azure Blob Storage setting
- Key Vault setting

The MongoDB, Blob Storage, and Key Vault values come from GitHub secrets and variables.

## Required GitHub Secrets

The workflow expects these secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `MONGODB_CONNECTION_STRING`
- `AZURE_BLOB_CONTAINER_URL`
- `KEY_VAULT_URI`

## Optional GitHub Variables

The workflow can use these repository variables:

- `AZURE_LOCATION`
- `AZURE_RESOURCE_GROUP`
- `ACA_PREFIX`
- `IMAGE_REPOSITORY`
- `USE_MONGODB`
- `USE_AZURE_STORAGE`
- `USE_AZURE_KEY_VAULT`

If they are missing, defaults are used for some values.

## Final Output

After deployment, the workflow reads this Bicep output:

```yaml
containerAppUrl
```

Then it prints:

```text
Deployed to <container-app-url>
```

## Complete Flow

1. Developer opens a pull request or pushes to `main`.
2. GitHub Actions checks out the code.
3. GitHub Actions installs .NET 10.
4. The solution is restored.
5. The solution is built.
6. Tests are run.
7. If this is a pull request, the workflow stops after tests.
8. If this is a push to `main`, deployment starts.
9. GitHub Actions logs in to Azure.
10. The Azure resource group is created or reused.
11. Azure Container Registry is created or updated.
12. Docker image is built.
13. Docker image is pushed to Azure Container Registry.
14. Azure Container Apps infrastructure is deployed.
15. The new container image is used by the Container App.
16. The workflow prints the deployed app URL.

## Troubleshooting: Azure Login Failed

You may see this error in GitHub Actions:

```text
Error: Login failed with Error: Using auth-type: SERVICE_PRINCIPAL.
Not all values are present. Ensure 'client-id' and 'tenant-id' are supplied.
```

This means the `azure/login@v2` step cannot find the Azure login secrets.

The workflow expects these secrets:

```yaml
client-id: ${{ secrets.AZURE_CLIENT_ID }}
tenant-id: ${{ secrets.AZURE_TENANT_ID }}
subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Fix Steps

1. Go to your GitHub repository.

2. Open:

```text
Settings -> Secrets and variables -> Actions
```

3. Click:

```text
New repository secret
```

4. Add these three secrets exactly with these names:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

5. Get the Azure tenant ID:

```bash
az account show --query tenantId -o tsv
```

Use that value for:

```text
AZURE_TENANT_ID
```

6. Get the Azure subscription ID:

```bash
az account show --query id -o tsv
```

Use that value for:

```text
AZURE_SUBSCRIPTION_ID
```

7. Create the Azure service principal:

```bash
az ad sp create-for-rbac \
  --name cloudsoft-github-actions \
  --role Contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)
```

Copy the `appId` value into:

```text
AZURE_CLIENT_ID
```

8. Configure federated credentials for GitHub Actions:

```text
Azure Portal
-> Microsoft Entra ID
-> App registrations
-> cloudsoft-github-actions
-> Federated credentials
-> Add credential
```

Use these settings:

```text
Federated credential scenario: GitHub Actions deploying Azure resources
Organization: your GitHub username or organization
Repository: your repository name
Entity type: Branch
Branch: main
```

9. Re-run the GitHub Actions workflow.

### Important

Do not add a client secret unless you also change the workflow to use `creds`.

The current workflow is configured for OIDC login. It needs:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

It does not need:

```text
AZURE_CLIENT_SECRET
```

## Troubleshooting: Role Assignment Write Failed

You may see this error during the `az deployment group create` step:

```text
Authorization failed for template resource '<role-assignment-id>'
of type 'Microsoft.Authorization/roleAssignments'.
The client '<client-id>' with object id '<object-id>' does not have permission
to perform action 'Microsoft.Authorization/roleAssignments/write'.
```

This means GitHub Actions can deploy Azure resources, but it cannot create Azure RBAC role assignments.

In this project, `infra/container-apps/main.bicep` creates an `AcrPull` role assignment so the Azure Container App managed identity can pull images from Azure Container Registry.

The GitHub service principal needs a role that includes:

```text
Microsoft.Authorization/roleAssignments/write
```

`Contributor` is not enough for that permission.

### Fix Steps

1. Find the GitHub Actions service principal object ID:

```bash
az ad sp show \
  --id <AZURE_CLIENT_ID> \
  --query id \
  -o tsv
```

2. Assign `User Access Administrator` at the deployment resource group scope:

```bash
az role assignment create \
  --assignee-object-id <SERVICE_PRINCIPAL_OBJECT_ID> \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP_NAME>
```

For this project, the command is:

```bash
az role assignment create \
  --assignee-object-id 306c236c-37f2-4720-8dc6-ee6916d66fd7 \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope /subscriptions/60d2ff53-a8ad-4abb-9856-7207f2958532/resourceGroups/cloudsoft-job-aca-rg
```

3. Confirm the service principal has both roles:

```bash
az role assignment list \
  --assignee 306c236c-37f2-4720-8dc6-ee6916d66fd7 \
  --all \
  --query "[].{role:roleDefinitionName,scope:scope}" \
  -o table
```

Expected roles:

```text
Contributor
User Access Administrator
```

4. Re-run the GitHub Actions workflow.

### Why This Fix Works

`Contributor` lets GitHub Actions create normal Azure resources.

`User Access Administrator` lets GitHub Actions create the `AcrPull` role assignment required by the Container App.

This is safer than assigning full `Owner` because it grants only role-assignment permission at the resource group scope.

## Troubleshooting: DeploymentActive Error

You may see this error during `az deployment group create`:

```text
DeploymentActive
The deployment ... cannot be saved, because this would overwrite an existing deployment which is still active.
```

This means an earlier Azure deployment with the same name is still running or stuck.

In this workflow, the Bicep deployment name is:

```text
main
```

Azure will not start another deployment named `main` while the previous `main` deployment is still active.

### Fix Steps

1. Check the deployment status:

```bash
az deployment group show \
  --resource-group cloudsoft-job-aca-rg \
  --name main \
  --query "{name:name, state:properties.provisioningState, timestamp:properties.timestamp, correlationId:properties.correlationId}" \
  -o json
```

2. If the state is `Running` and it is stuck, cancel it:

```bash
az deployment group cancel \
  --resource-group cloudsoft-job-aca-rg \
  --name main
```

3. Confirm it is canceled:

```bash
az deployment group show \
  --resource-group cloudsoft-job-aca-rg \
  --name main \
  --query "properties.provisioningState" \
  -o tsv
```

Expected result:

```text
Canceled
```

4. Re-run the GitHub Actions workflow.

### Optional Prevention

Use a unique deployment name in the workflow so two runs do not conflict.

Example:

```bash
az deployment group create \
  --name "main-${{ github.run_id }}" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --template-file infra/container-apps/main.bicep
```

This avoids name conflicts, but keeping the name `main` is also fine if old stuck deployments are canceled before retrying.
