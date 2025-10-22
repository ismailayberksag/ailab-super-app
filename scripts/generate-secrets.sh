#!/bin/bash

# Güvenli random secret'lar oluşturan yardımcı script

echo "🔐 Güvenli Secret'lar Oluşturuluyor..."
echo ""

echo "JWT_SECRET (64 karakter):"
openssl rand -base64 48
echo ""

echo "POSTGRES_PASSWORD (32 karakter):"
openssl rand -base64 24
echo ""

echo "Örnek .env yapısı:"
cat << 'ENVFILE'
POSTGRES_DB=ailab_db
POSTGRES_USER=ailab_user
POSTGRES_PASSWORD=$(openssl rand -base64 24)
POSTGRES_PORT=5432

ASPNETCORE_ENVIRONMENT=Production
API_PORT=6161

JWT_SECRET=$(openssl rand -base64 48)
JWT_ISSUER=https://api.ailab.org.tr
JWT_AUDIENCE=https://api.ailab.org.tr
JWT_ACCESS_TOKEN_EXPIRATION=60
JWT_REFRESH_TOKEN_EXPIRATION=7
ENVFILE

echo ""
echo "✅ Yukarıdaki değerleri GitHub Secrets'a ekleyin!"
