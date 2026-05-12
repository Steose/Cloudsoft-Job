# Azure Container Registry Image Push

## Short Description

The CI/CD workflow now builds the Cloudsoft-Job Docker image and pushes it to Azure Container Registry.

The image is built from `docker/Dockerfile`.

## Implementation Flow

1. GitHub Actions deploys `infra/container-apps/registry.bicep`.
2. The Bicep deployment outputs:
   - ACR name
   - ACR login server
3. GitHub Actions logs in to ACR with:

```bash
az acr login --name "$ACR_NAME"
```

4. Docker builds the image:

```bash
docker build --file docker/Dockerfile .
```

5. The workflow tags the image as:

```text
<acr-login-server>/cloudsoft-job:<commit-sha>
<acr-login-server>/cloudsoft-job:latest
```

6. The workflow pushes both tags to ACR.

## Main Files

- `docker/Dockerfile`
- `infra/container-apps/registry.bicep`
- `.github/workflows/ci-cd.yml`

## Why Two Tags Are Used

- The commit SHA tag points to the exact version deployed.
- The `latest` tag is convenient for quick inspection and manual testing.

The Container App deployment uses the commit SHA tag so deployments are traceable.
