# Azure Deployment Plan

Status: Ready for Validation

## Goal

Implement repository-ready Azure Container Apps deployment support for the existing Cloudsoft-Job application.

## Scope

- Add Azure Container Apps infrastructure.
- Add Azure Container Registry image push flow.
- Add GitHub Actions deployment from `main` to Azure.
- Add short Markdown descriptions for each requested deployment area.

## Initial Assumptions

- Existing ASP.NET Core MVC app remains the deployed workload.
- Existing `docker/Dockerfile` remains the production container build file.
- GitHub Actions will authenticate to Azure using OIDC federated credentials.
- Deployment artifacts are prepared in the repository; actual Azure deployment is not executed in this turn.

## Plan

1. [x] Inspect existing Docker, CI/CD, app configuration, and infrastructure.
2. [x] Add Bicep infrastructure for Azure Container Registry, Log Analytics, Container Apps Environment, and Container App.
3. [x] Add GitHub Actions deploy job that builds and pushes the image to ACR, deploys infrastructure, and updates the Container App image.
4. [x] Add short Markdown implementation flow files for:
   - Azure Container Apps deployment
   - Azure Container Registry image push
   - GitHub Actions deploy from `main`
5. [x] Validate with local formatting/build checks where possible.

## Validation Completed

- `az bicep build --file infra/container-apps/registry.bicep`
- `az bicep build --file infra/container-apps/main.bicep`
- `dotnet test Cloudsoft.sln`

## Pending Decisions

- Azure subscription ID, tenant ID, and resource group names must be supplied as GitHub repository variables/secrets.
- Production MongoDB and API key secrets must be supplied through GitHub secrets or Key Vault.
