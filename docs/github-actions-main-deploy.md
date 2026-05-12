# GitHub Actions Deploy From Main

## Short Description

The GitHub Actions workflow now deploys Cloudsoft-Job to Azure Container Apps when code is pushed to `main`.

Pull requests still run build and tests only.

## Implementation Flow

1. A pull request runs:
   - restore
   - build
   - tests

2. A push to `main` runs:
   - restore
   - build
   - tests
   - Azure login
   - resource group creation
   - ACR creation/update
   - Docker image build
   - Docker image push to ACR
   - Azure Container App deployment

3. Azure login uses OIDC with:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

4. The deploy job creates or updates Azure resources with Bicep.

5. The deploy job prints the public Container App URL after deployment.

## Main File

- `.github/workflows/ci-cd.yml`

## Required Azure Setup

Before the workflow can deploy, Azure must have an Entra app registration or managed identity configured for GitHub OIDC federation.

That identity needs permission to create and update resources in the target resource group or subscription.

## Required GitHub Environment

The deploy job uses the `production` environment.

Configure secrets and variables in GitHub before the first deployment.
