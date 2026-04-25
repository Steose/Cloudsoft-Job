

#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TEMPLATE_FILE="${SCRIPT_DIR}/main.bicep"
PARAMETERS_FILE="${SCRIPT_DIR}/main.bicepparam"
WEB_PROJECT_FILE="${REPO_ROOT}/src/Cloudsoft.Web/Cloudsoft.Web.csproj"
PUBLISH_DIR="${REPO_ROOT}/publish"
SETUP_APP_SCRIPT="${SCRIPT_DIR}/setup-app-vm.sh"
SETUP_PROXY_SCRIPT="${SCRIPT_DIR}/setup-proxy-vm.sh"
SSH_KEY_PATH="${SSH_PRIVATE_KEY_PATH:-${HOME}/.ssh/id_ed25519}"
ADMIN_USERNAME="${ADMIN_USERNAME:-azureuser}"
SSH_OPTIONS=(
  -i "${SSH_KEY_PATH}"
  -o StrictHostKeyChecking=no
  -o UserKnownHostsFile=/dev/null
)

RESOURCE_GROUP="CloudsoftJobRG"
LOCATION="northeurope"
DEPLOYMENT_NAME=""

wait_for_ssh() {
  local host="$1"
  shift

  for attempt in {1..30}; do
    if ssh "${SSH_OPTIONS[@]}" "$@" "${ADMIN_USERNAME}@${host}" "echo ready" >/dev/null 2>&1; then
      return 0
    fi

    echo "Waiting for SSH on ${host} (attempt ${attempt}/30)..."
    sleep 10
  done

  echo "SSH did not become ready for ${host}."
  return 1
}

urlencode() {
  python3 -c 'import sys, urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=""))' "$1"
}

echo "Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION"

echo "Getting signed-in user object ID..."
CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show --query id --output tsv)

echo "Deploying Bicep..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$TEMPLATE_FILE" \
  --parameters "$PARAMETERS_FILE" \
  --parameters currentUserObjectId="$CURRENT_USER_OBJECT_ID"

DEPLOYMENT_NAME=$(az deployment group list \
  --resource-group "$RESOURCE_GROUP" \
  --query "[0].name" \
  --output tsv)

echo "Deployment complete."

echo "Outputs:"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs"

BASTION_PUBLIC_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.bastionPublicIp.value" \
  --output tsv)

REVERSE_PROXY_PUBLIC_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.reverseProxyPublicIp.value" \
  --output tsv)

APP_PRIVATE_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.appPrivateIp.value" \
  --output tsv)

PROXY_PRIVATE_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.reverseProxyPrivateIp.value" \
  --output tsv)

COSMOS_ACCOUNT_NAME=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.cosmosAccountNameOutput.value" \
  --output tsv)

COSMOS_DB_NAME=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.cosmosMongoDbNameOutput.value" \
  --output tsv)

if [[ ! -f "${SSH_KEY_PATH}" ]]; then
  echo "SSH key not found: ${SSH_KEY_PATH}"
  echo "Set SSH_PRIVATE_KEY_PATH to the private key that matches infra/main.bicepparam."
  exit 1
fi

if [[ ! -x "${SETUP_APP_SCRIPT}" || ! -x "${SETUP_PROXY_SCRIPT}" ]]; then
  echo "Setup scripts are missing or not executable."
  exit 1
fi

echo
echo "Building Mongo connection string..."
COSMOS_PRIMARY_KEY=$(az cosmosdb keys list \
  --resource-group "$RESOURCE_GROUP" \
  --name "$COSMOS_ACCOUNT_NAME" \
  --type keys \
  --query primaryMasterKey \
  --output tsv)

COSMOS_USERNAME_ENCODED=$(urlencode "$COSMOS_ACCOUNT_NAME")
COSMOS_PASSWORD_ENCODED=$(urlencode "$COSMOS_PRIMARY_KEY")
MONGODB_CONNECTION_STRING="mongodb://${COSMOS_USERNAME_ENCODED}:${COSMOS_PASSWORD_ENCODED}@${COSMOS_ACCOUNT_NAME}.mongo.cosmos.azure.com:10255/${COSMOS_DB_NAME}?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&authMechanism=SCRAM-SHA-256&appName=@${COSMOS_ACCOUNT_NAME}@"
MONGODB_CONNECTION_STRING_B64=$(printf '%s' "${MONGODB_CONNECTION_STRING}" | base64)

echo
echo "Publishing the app..."
rm -rf "${PUBLISH_DIR}"
dotnet publish "${WEB_PROJECT_FILE}" -c Release -o "${PUBLISH_DIR}"

echo
echo "Waiting for bastion SSH..."
wait_for_ssh "${BASTION_PUBLIC_IP}"

echo
echo "Waiting for app VM SSH through the bastion..."
wait_for_ssh "${APP_PRIVATE_IP}" -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}"

echo
echo "Waiting for proxy VM SSH through the bastion..."
wait_for_ssh "${PROXY_PRIVATE_IP}" -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}"

echo
echo "Configuring the proxy VM..."
scp "${SSH_OPTIONS[@]}" \
  -o ProxyJump="${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${SETUP_PROXY_SCRIPT}" \
  "${ADMIN_USERNAME}@${PROXY_PRIVATE_IP}:/tmp/setup-proxy-vm.sh"

ssh "${SSH_OPTIONS[@]}" \
  -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${ADMIN_USERNAME}@${PROXY_PRIVATE_IP}" \
  "chmod +x /tmp/setup-proxy-vm.sh && sudo env APP_PRIVATE_IP='${APP_PRIVATE_IP}' bash /tmp/setup-proxy-vm.sh"

echo
echo "Configuring the app VM..."
scp "${SSH_OPTIONS[@]}" \
  -o ProxyJump="${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${SETUP_APP_SCRIPT}" \
  "${ADMIN_USERNAME}@${APP_PRIVATE_IP}:/tmp/setup-app-vm.sh"

ssh "${SSH_OPTIONS[@]}" \
  -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${ADMIN_USERNAME}@${APP_PRIVATE_IP}" \
  "chmod +x /tmp/setup-app-vm.sh && sudo env MONGODB_CONNECTION_STRING_B64='${MONGODB_CONNECTION_STRING_B64}' bash /tmp/setup-app-vm.sh"

echo
echo "Copying published files to the app VM..."
scp "${SSH_OPTIONS[@]}" \
  -o ProxyJump="${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  -r "${PUBLISH_DIR}/"* \
  "${ADMIN_USERNAME}@${APP_PRIVATE_IP}:/opt/cloudsoft/"

echo
echo "Refreshing systemd and starting the app..."
ssh "${SSH_OPTIONS[@]}" \
  -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${ADMIN_USERNAME}@${APP_PRIVATE_IP}" \
  "sudo systemctl daemon-reload && if sudo systemctl list-unit-files | grep -q '^cloudsoft-web.path'; then sudo systemctl restart cloudsoft-web.path; else sudo systemctl restart cloudsoft-web.service; fi"

echo
echo "Verifying the app VM..."
ssh "${SSH_OPTIONS[@]}" \
  -J "${ADMIN_USERNAME}@${BASTION_PUBLIC_IP}" \
  "${ADMIN_USERNAME}@${APP_PRIVATE_IP}" \
  "/opt/cloudsoft/verify-cloudsoft.sh"

echo
echo "Verifying the reverse proxy..."
curl --fail --show-error --silent --location "http://${REVERSE_PROXY_PUBLIC_IP}" >/dev/null

echo
echo "Deployment succeeded."
echo "Bastion public IP: ${BASTION_PUBLIC_IP}"
echo "Reverse proxy URL: http://${REVERSE_PROXY_PUBLIC_IP}"
echo "Reverse proxy private IP: ${PROXY_PRIVATE_IP}"
echo "App private IP: ${APP_PRIVATE_IP}"
echo
echo "If you need to inspect the app VM again:"
echo "  ssh -i ${SSH_KEY_PATH} -J ${ADMIN_USERNAME}@${BASTION_PUBLIC_IP} ${ADMIN_USERNAME}@${APP_PRIVATE_IP} '/opt/cloudsoft/verify-cloudsoft.sh'"
echo "If you need to inspect the proxy VM again:"
echo "  ssh -i ${SSH_KEY_PATH} -J ${ADMIN_USERNAME}@${BASTION_PUBLIC_IP} ${ADMIN_USERNAME}@${PROXY_PRIVATE_IP} 'curl -i --max-time 10 http://${APP_PRIVATE_IP}:8080 && sudo systemctl status nginx --no-pager'"
echo
echo "Note: Key Vault RBAC role assignment may take a short time to propagate."
