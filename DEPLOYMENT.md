# 🚀 Deployment Rehberi

## Production Deployment Adımları

### Yöntem 1: GitHub Actions ile Otomatik Deployment (Önerilen)

#### Ön Hazırlık

1. **SSH Key Oluşturma**
```bash
# Lokal bilgisayarınızda
ssh-keygen -t ed25519 -C "github-actions-ailab" -f ~/.ssh/github_actions_ailab

# Public key'i sunucuya kopyala
ssh-copy-id -i ~/.ssh/github_actions_ailab.pub user@sunucu-ip
```

2. **GitHub Secrets Ayarlama**

GitHub Repository → Settings → Secrets and variables → Actions

| Secret Name | Açıklama | Örnek |
|------------|----------|-------|
| `POSTGRES_DB` | Database adı | `ailab_db` |
| `POSTGRES_USER` | Database kullanıcısı | `ailab_user` |
| `POSTGRES_PASSWORD` | Database şifresi | `Gu@123!Ab*` |
| `JWT_SECRET` | JWT secret key (min 32 karakter) | `openssl rand -base64 48` |
| `JWT_ISSUER` | JWT issuer | `https://api.ailab.org.tr` |
| `JWT_AUDIENCE` | JWT audience | `https://api.ailab.org.tr` |
| `JWT_ACCESS_TOKEN_EXPIRATION` | Access token süresi (dakika) | `60` |
| `JWT_REFRESH_TOKEN_EXPIRATION` | Refresh token süresi (gün) | `7` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |
| `SERVER_HOST` | Sunucu IP/domain | `123.456.789.0` |
| `SERVER_USER` | SSH kullanıcı adı | `root` veya `admin` |
| `SERVER_PORT` | SSH port | `22` |
| `SSH_PRIVATE_KEY` | Private key içeriği | `cat ~/.ssh/github_actions_ailab` |

3. **Sunucuda İlk Kurulum**

```bash
# SSH ile sunucuya bağlan
ssh user@sunucu-ip

# Docker ve Docker Compose kur (yoksa)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Git kur
sudo apt update
sudo apt install -y git

# Proje dizini oluştur
sudo mkdir -p /var/www/ailab-api
sudo chown $USER:$USER /var/www/ailab-api
cd /var/www/ailab-api

# Repository'yi clone et
git clone https://github.com/KULLANICI_ADINIZ/ailab-super-app.git .
```

4. **Deployment Tetikleme**

```bash
# main branch'e push yap
git push origin main

# Veya GitHub'da Actions sekmesinden manuel tetikle
# Repository → Actions → Deploy to Production → Run workflow
```

---

### Yöntem 2: Manuel Deployment

#### 1. Sunucuda Hazırlık

```bash
# Proje dizinine git
cd /var/www/ailab-api

# Son değişiklikleri çek
git pull origin main

# .env dosyasını oluştur (ilk seferde)
nano .env
```

**.env dosyası içeriği:**
```env
POSTGRES_DB=ailab_db
POSTGRES_USER=ailab_user
POSTGRES_PASSWORD=GÜÇLÜ-ŞİFRE-BURAYA
POSTGRES_PORT=5432

ASPNETCORE_ENVIRONMENT=Production
API_PORT=6161

JWT_SECRET=EN-AZ-32-KARAKTER-UZUNLUĞUNDA-RASTGELE-KEY
JWT_ISSUER=https://api.ailab.org.tr
JWT_AUDIENCE=https://api.ailab.org.tr
JWT_ACCESS_TOKEN_EXPIRATION=60
JWT_REFRESH_TOKEN_EXPIRATION=7
```

#### 2. Docker ile Başlatma

```bash
# Container'ları başlat
docker compose up -d --build

# Logları izle
docker compose logs -f api

# Container durumunu kontrol et
docker compose ps

# Sağlık kontrolü
curl http://localhost:6161/swagger/index.html
```

---

### Yöntem 3: CloudPanel ile Deployment

#### 1. CloudPanel'de Site Oluşturma

1. CloudPanel'e giriş yap
2. **Sites** → **Add Site**
3. **Site Type:** Reverse Proxy
4. **Domain:** `api.ailab.org.tr`
5. **Reverse Proxy URL:** `http://localhost:6161`
6. **SSL:** Let's Encrypt seçin (otomatik)

#### 2. CloudPanel Nginx Konfigürasyonu

CloudPanel otomatik yapılandırır, ancak manuel düzenleme gerekirse:

```nginx
location / {
    proxy_pass http://localhost:6161;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_cache_bypass $http_upgrade;
    
    # Timeout ayarları (büyük dosya upload için)
    proxy_connect_timeout 60s;
    proxy_send_timeout 60s;
    proxy_read_timeout 60s;
}
```

---

## 🔄 Güncelleme ve Bakım

### Kod Güncellemesi

```bash
# Sunucuda
cd /var/www/ailab-api
git pull origin main
docker compose up -d --build
```

### Database Migration

Migration'lar otomatik çalışır (Program.cs'de `db.Database.Migrate()`).

Manuel migration gerekirse:

```bash
# Container içinde
docker exec -it ailab-api dotnet ef database update
```

### Backup

```bash
# Database backup
docker exec ailab-postgres pg_dump -U postgres ailab_super_app > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore
docker exec -i ailab-postgres psql -U postgres ailab_super_app < backup.sql
```

### Log İzleme

```bash
# Tüm loglar
docker compose logs -f

# Sadece API
docker compose logs -f api

# Sadece PostgreSQL
docker compose logs -f postgres

# Son 100 satır
docker compose logs --tail=100 api
```

---

## 🔐 Güvenlik Best Practices

1. **Firewall Ayarları**
```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp
sudo ufw enable
```

2. **SSH Güvenliği**
```bash
# /etc/ssh/sshd_config
PermitRootLogin no
PasswordAuthentication no
PubkeyAuthentication yes
```

3. **Secret Rotation**
```bash
# JWT secret güncelleme
# .env dosyasında JWT_SECRET'ı değiştir
docker compose restart api
```

4. **SSL Sertifikası Yenileme**

Let's Encrypt otomatik yenilenir. Manuel kontrol:
```bash
sudo certbot renew --dry-run
```

---

## 🐛 Troubleshooting

### API Başlamıyor

```bash
# Logları kontrol et
docker compose logs api

# Container'ı yeniden başlat
docker compose restart api

# Tamamen rebuild
docker compose down
docker compose up -d --build
```

### Database Bağlantı Hatası

```bash
# PostgreSQL logları
docker compose logs postgres

# Database durumu
docker exec ailab-postgres psql -U postgres -c "\l"

# Connection string kontrol
cat .env | grep POSTGRES
```

### Port Çakışması

```bash
# 6161 portunu kullanan process'i bul
sudo lsof -i :6161

# Process'i durdur
sudo kill -9 <PID>
```

---

## 📊 Monitoring

### Container Health Check

```bash
# Sağlık durumu
docker compose ps

# Health check detayları
docker inspect --format='{{json .State.Health}}' ailab-api | jq
```

### Resource Kullanımı

```bash
# CPU ve Memory
docker stats ailab-api ailab-postgres

# Disk kullanımı
docker system df
```

---

## 🎯 Production Checklist

- [ ] `.env` dosyası güvenli şifrelerle oluşturuldu
- [ ] GitHub Secrets ayarlandı
- [ ] SSH key'ler oluşturuldu ve sunucuya eklendi
- [ ] Firewall kuralları ayarlandı
- [ ] CloudPanel reverse proxy kuruldu
- [ ] Let's Encrypt SSL aktif
- [ ] Docker container'lar başarıyla çalışıyor
- [ ] API endpoint'leri test edildi
- [ ] Database migration'lar çalıştı
- [ ] Log monitoring aktif
- [ ] Backup stratejisi belirlendi

