#!/usr/bin/env bash
set -euo pipefail

APP_PRIVATE_IP="${APP_PRIVATE_IP:-10.0.2.4}"

apt-get update
apt-get install -y nginx

cat > /etc/nginx/sites-available/default <<EOF
server {
  listen 80 default_server;
  listen [::]:80 default_server;

  server_name _;

  location / {
    proxy_pass http://${APP_PRIVATE_IP}:8080/;
    proxy_http_version 1.1;
    proxy_set_header Host \$host;
    proxy_set_header X-Real-IP \$remote_addr;
    proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto \$scheme;
  }
}
EOF

systemctl daemon-reload
systemctl enable nginx
systemctl restart nginx
