# vm-deploy folder

This folder contains the Azure infrastructure and deployment scripts for the Cloudsoft app.

## How the files fit together

1. `main.bicep` is the main infrastructure template.
   It creates the Azure resources: virtual network, subnets, security rules, public IPs, VMs, Cosmos DB, Key Vault, and deployment outputs.

2. `main.bicepparam` provides values for `main.bicep`.
   It sets things like the Azure region, resource name prefix, VM size, SSH public key, Cosmos DB name, and Key Vault name.

3. `provision.sh` is the main script to run.
   It creates the resource group, deploys `main.bicep` with `main.bicepparam`, reads the deployment outputs, builds the app, configures the VMs, copies the published app files, and verifies the deployment.

4. `setup-proxy-vm.sh` configures the reverse proxy VM.
   It installs Nginx and forwards public HTTP traffic from port `80` to the private app VM on port `8080`.

5. `setup-app-vm.sh` configures the app VM.
   It installs the ASP.NET Core runtime, writes the MongoDB environment settings, creates the `cloudsoft-web` systemd service, and starts the app when `Cloudsoft.Web.dll` is present.

6. `cloud-init-nginx.yaml` is an alternate cloud-init version of the proxy setup.
   It installs and configures Nginx during VM startup, but it is not currently referenced by `main.bicep`.

7. `cloud-init-dotnet-app.yaml` is an alternate cloud-init version of the app setup.
   It installs .NET and creates the app service during VM startup, but it is not currently referenced by `main.bicep`.

## Deployment flow

1. Run `provision.sh`.
2. Azure deploys the resources from `main.bicep`.
3. The bastion VM gets a public IP for SSH access.
4. The proxy VM gets a public IP for web traffic.
5. The app VM stays private and only accepts traffic from the proxy VM and SSH from the bastion VM.
6. Cosmos DB stores the application data.
7. Key Vault is created for secrets and future secure configuration.
8. `provision.sh` publishes the .NET app locally.
9. `setup-proxy-vm.sh` configures Nginx on the proxy VM.
10. `setup-app-vm.sh` configures the .NET service on the app VM.
11. The published app is copied to `/opt/cloudsoft` on the app VM.
12. The reverse proxy URL serves the app through Nginx.

## Network relationship

Internet users reach only the reverse proxy VM on port `80`.
The proxy VM forwards requests to the private app VM on port `8080`.
The app VM connects to Cosmos DB for data.
SSH access to private VMs goes through the bastion VM.
