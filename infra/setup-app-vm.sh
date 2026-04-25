#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${MONGODB_CONNECTION_STRING_B64:-}" ]]; then
  echo "MONGODB_CONNECTION_STRING_B64 is required."
  exit 1
fi

APP_DIR="/opt/cloudsoft"
SERVICE_FILE="/etc/systemd/system/cloudsoft-web.service"
PATH_FILE="/etc/systemd/system/cloudsoft-web.path"
ENV_DIR="/etc/cloudsoft"
ENV_FILE="${ENV_DIR}/cloudsoft-web.env"
DOTNET_INSTALL_DIR="/usr/share/dotnet"

mkdir -p "${APP_DIR}" "${ENV_DIR}"
chown -R azureuser:azureuser "${APP_DIR}"

if ! command -v dotnet >/dev/null 2>&1 && [[ ! -x "${DOTNET_INSTALL_DIR}/dotnet" ]]; then
  apt-get update
  apt-get install -y software-properties-common ca-certificates wget gpg curl

  if ! apt-cache show aspnetcore-runtime-10.0 >/dev/null 2>&1; then
    add-apt-repository -y ppa:dotnet/backports
    apt-get update
  fi

  apt-get install -y aspnetcore-runtime-10.0 || true
fi

if ! command -v dotnet >/dev/null 2>&1 && [[ ! -x "${DOTNET_INSTALL_DIR}/dotnet" ]]; then
  mkdir -p "${DOTNET_INSTALL_DIR}"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir "${DOTNET_INSTALL_DIR}"
  rm -f /tmp/dotnet-install.sh
fi

if [[ -x "${DOTNET_INSTALL_DIR}/dotnet" && ! -x /usr/bin/dotnet ]]; then
  ln -sf "${DOTNET_INSTALL_DIR}/dotnet" /usr/bin/dotnet
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet was not installed successfully."
  exit 1
fi

MONGODB_CONNECTION_STRING="$(printf '%s' "${MONGODB_CONNECTION_STRING_B64}" | base64 -d)"

cat > "${ENV_FILE}" <<EOF
ASPNETCORE_URLS=http://0.0.0.0:8080
ASPNETCORE_ENVIRONMENT=Production
FeatureFlags__UseMongoDb=true
MongoDb__ConnectionString=${MONGODB_CONNECTION_STRING}
MongoDb__DatabaseName=Cloudsoft
MongoDb__JobPostingsCollectionName=jobPostings
MongoDb__EmployersCollectionName=employers
DOTNET_ROOT=${DOTNET_INSTALL_DIR}
PATH=${DOTNET_INSTALL_DIR}:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
EOF

cat > "${SERVICE_FILE}" <<'EOF'
[Unit]
Description=Cloudsoft Web ASP.NET Core Application
After=network.target
ConditionPathExists=/opt/cloudsoft/Cloudsoft.Web.dll

[Service]
Type=simple
User=azureuser
Group=azureuser
WorkingDirectory=/opt/cloudsoft
EnvironmentFile=/etc/cloudsoft/cloudsoft-web.env
ExecStart=/usr/bin/env dotnet /opt/cloudsoft/Cloudsoft.Web.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=cloudsoft-web

[Install]
WantedBy=multi-user.target
EOF

cat > "${PATH_FILE}" <<'EOF'
[Unit]
Description=Start Cloudsoft Web when the published app is deployed

[Path]
PathExists=/opt/cloudsoft/Cloudsoft.Web.dll
Unit=cloudsoft-web.service

[Install]
WantedBy=multi-user.target
EOF

cat > "${APP_DIR}/deploy-cloudsoft.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

APP_DIR="/opt/cloudsoft"

echo "Cloudsoft deployment directory: ${APP_DIR}"
echo "Expected published entrypoint: ${APP_DIR}/Cloudsoft.Web.dll"
echo
echo "Publish locally from the repository root with:"
echo "  dotnet publish src/Cloudsoft.Web/Cloudsoft.Web.csproj -c Release -o ./publish"
echo "Or from infra/ with:"
echo "  dotnet publish ../src/Cloudsoft.Web/Cloudsoft.Web.csproj -c Release -o ./publish"
echo
echo "Copy files to the VM with:"
echo "  scp -r ./publish/* azureuser@<vm-ip>:${APP_DIR}/"
echo
echo "The VM is configured to auto-start the app when ${APP_DIR}/Cloudsoft.Web.dll appears."
echo "If you want to force a rescan after copying files:"
echo "  ssh azureuser@<vm-ip> 'sudo systemctl restart cloudsoft-web.path'"
echo
echo "Check service health with:"
echo "  ssh azureuser@<vm-ip> 'systemctl status cloudsoft-web --no-pager'"
echo "  ssh azureuser@<vm-ip> 'journalctl -u cloudsoft-web -n 100 --no-pager'"
EOF

cat > "${APP_DIR}/verify-cloudsoft.sh" <<'EOF'
#!/usr/bin/env bash
set -u

echo "== File check =="
ls -la /opt/cloudsoft
echo

echo "== systemd path unit =="
sudo systemctl status cloudsoft-web.path --no-pager || true
echo

echo "== systemd service =="
sudo systemctl status cloudsoft-web --no-pager || true
echo

echo "== Recent service logs =="
sudo journalctl -u cloudsoft-web -n 100 --no-pager || true
echo

echo "== Local HTTP probe =="
curl -i --max-time 10 http://localhost:8080 || true
echo

echo "== Listening sockets =="
sudo ss -ltnp | grep 8080 || true
EOF

chmod 0644 "${SERVICE_FILE}" "${PATH_FILE}" "${ENV_FILE}"
chmod 0755 "${APP_DIR}/deploy-cloudsoft.sh" "${APP_DIR}/verify-cloudsoft.sh"
chown azureuser:azureuser "${APP_DIR}/deploy-cloudsoft.sh" "${APP_DIR}/verify-cloudsoft.sh"

systemctl daemon-reload
systemctl enable cloudsoft-web.path
systemctl start cloudsoft-web.path
