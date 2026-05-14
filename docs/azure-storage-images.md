# Azure Storage Images

## Short Description

The Azure Container Apps Bicep deployment now creates an Azure Storage Account and a blob container named `images`.

The web app can use this container for public image files such as `hero.png`.

## Implementation Flow

1. Set `useAzureStorage` to `true` in the Container Apps deployment.
2. `infra/container-apps/main.bicep` creates a StorageV2 account.
3. The Bicep file enables public blob access on the storage account.
4. The Bicep file creates the `images` blob container.
5. The container allows public read access for blobs.
6. Bicep outputs the generated storage account, container, and container URL.
7. GitHub Actions grants Storage Blob Data Contributor to the GitHub OIDC deployment principal.
8. GitHub Actions uploads `src/Cloudsoft.Web/wwwroot/images/hero.png` to the container.
9. The Container App receives that URL as:

```text
AzureBlob__ContainerUrl
```

10. The app builds image URLs from that container URL.

For example:

```text
https://<storage-account>.blob.core.windows.net/images/hero.png
```

## Main Files

- `infra/container-apps/main.bicep`
- `infra/container-apps/main.bicepparam`
- `.github/workflows/ci-cd.yml`

## Uploading Images

The CI/CD workflow uploads `hero.png` automatically after `infra/container-apps/main.bicep` is deployed. Set the `AZURE_CLIENT_OBJECT_ID` GitHub variable to the object ID of the service principal used by `AZURE_CLIENT_ID`; the workflow uses that object ID to assign `Storage Blob Data Contributor` without an Entra service principal lookup.

You can get the object ID with:

```bash
az ad sp show --id <AZURE_CLIENT_ID> --query id --output tsv
```

For manual uploads, use:

```bash
az storage blob upload \
  --account-name <storage-account-name> \
  --container-name images \
  --name hero.png \
  --file src/Cloudsoft.Web/wwwroot/images/hero.png \
  --auth-mode login \
  --overwrite
```

## Notes

The image container is public because the browser needs to load static image files directly.

Do not store secrets or private files in this container.
