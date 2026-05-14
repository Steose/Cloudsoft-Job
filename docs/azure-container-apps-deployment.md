# Azure Container Apps Deployment

## Short Description

Cloudsoft-Job can now be deployed as an Azure Container App using the Bicep files in `infra/container-apps/`.

This deployment path is separate from the existing VM deployment in `infra/`, so the old VM scripts remain available.

## Implementation Flow

1. GitHub Actions logs in to Azure using OIDC.
2. The workflow creates or updates the Azure resource group.
3. `infra/container-apps/registry.bicep` creates or updates Azure Container Registry.
4. The workflow builds the Docker image from `docker/Dockerfile`.
5. The workflow pushes the image to ACR with two tags:
   - the commit SHA
   - `latest`
6. `infra/container-apps/main.bicep` creates or updates:
   - Log Analytics workspace
   - Cosmos DB account using the MongoDB API
   - MongoDB database used by the app
   - Storage account and `images` blob container
   - Azure Container Apps managed environment
   - User-assigned managed identity
   - ACR pull role assignment
   - Azure Container App
7. The Container App runs the pushed image on port `8080`.
8. The app receives a generated Cosmos DB MongoDB connection string through the `MongoDb__ConnectionString` secret.
9. The app receives the generated image container URL through `AzureBlob__ContainerUrl`.
10. The workflow prints the public Container App URL.

## Main Files

- `infra/container-apps/main.bicep`
- `infra/container-apps/main.bicepparam`
- `.github/workflows/ci-cd.yml`

## Required GitHub Secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Optional secrets:

- `MONGODB_CONNECTION_STRING` only when overriding the Cosmos DB account created by the template
- `AZURE_BLOB_CONTAINER_URL`
- `KEY_VAULT_URI`

## Required GitHub Variables

The workflow has defaults, but these variables can be set in GitHub for clearer production configuration:

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`
- `ACA_PREFIX`
- `IMAGE_REPOSITORY`
- `USE_MONGODB`
- `USE_AZURE_STORAGE`
- `USE_AZURE_KEY_VAULT`

## Notes

The Container App uses a user-assigned managed identity to pull images from ACR. ACR admin credentials are disabled.

The Container Apps template provisions Cosmos DB with the MongoDB API and enables MongoDB-backed repositories by default, so data persists across restarts, new revisions, and scale events. If `MONGODB_CONNECTION_STRING` is supplied, that external MongoDB connection string overrides the generated Cosmos DB connection string.

When `USE_AZURE_STORAGE` is `true`, the deployment creates a StorageV2 account and a public blob container named `images`. Upload `hero.png` to that container so the web app can load `images/hero.png` from Azure Blob Storage.
