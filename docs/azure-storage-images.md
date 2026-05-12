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
6. Bicep outputs the generated container URL.
7. The Container App receives that URL as:

```text
AzureBlob__ContainerUrl
```

8. The app builds image URLs from that container URL.

For example:

```text
https://<storage-account>.blob.core.windows.net/images/hero.png
```

## Main Files

- `infra/container-apps/main.bicep`
- `infra/container-apps/main.bicepparam`
- `.github/workflows/ci-cd.yml`

## Uploading Images

After deployment, upload `hero.png` to the `images` container.

Example:

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
