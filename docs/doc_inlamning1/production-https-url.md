# Production HTTPS URL

There are two production options for getting a secure HTTPS URL.

## Option 1: Azure Container Apps

Azure Container Apps gives an HTTPS URL automatically.

If Azure says the subscription is not registered for `Microsoft.App`, register the required resource providers first:

```bash
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.OperationalInsights --wait
```

Check that both providers are registered:

```bash
az provider show -n Microsoft.App --query registrationState -o tsv
az provider show -n Microsoft.OperationalInsights --query registrationState -o tsv
```

Expected output:

```text
Registered
```

Get the HTTPS URL:

```bash
az containerapp show \
  --name cloudsoftjob-web \
  --resource-group cloudsoft-job-aca-rg \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

Then open:

```text
https://<fqdn>
```

Example format:

```text
https://cloudsoftjob-web.<random>.<region>.azurecontainerapps.io
```

If the Container App does not exist yet, re-run the GitHub Actions workflow after registering the providers.

## Option 2: VM Deployment

The VM deployment currently uses HTTP:

```text
http://20.166.54.228
```

That is not secure because it does not use HTTPS.

To get HTTPS for the VM deployment, use a domain name.

Example:

```text
cloudsoftjob.com
```

Point the domain DNS `A` record to the proxy VM public IP:

```text
20.166.54.228
```

Then install a certificate on the proxy VM using Nginx and Certbot:

```bash
ssh -i ~/.ssh/id_ed25519 azureuser@20.166.54.228
```

On the proxy VM:

```bash
sudo apt update
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

After that, the secure production URL becomes:

```text
https://your-domain.com
```

## Important

You normally cannot get a trusted HTTPS certificate for only an IP address like:

```text
https://20.166.54.228
```

Use a domain name for the VM deployment, or use the automatic Azure Container Apps HTTPS URL.
