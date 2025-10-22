#!/bin/bash

# Secret dosyalarının Git'e eklenmediğini kontrol eden script

echo "🔍 Secret dosyaları kontrol ediliyor..."

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Git tracked dosyalarında hassas bilgi kontrolü
echo ""
echo "📁 Git tracked dosyalar kontrol ediliyor..."

FOUND_SECRETS=0

# .env dosyası kontrolü
if git ls-files --error-unmatch .env &> /dev/null; then
    echo -e "${RED}❌ HATA: .env dosyası Git'e eklenmiş!${NC}"
    FOUND_SECRETS=1
else
    echo -e "${GREEN}✅ .env dosyası Git'te yok (doğru)${NC}"
fi

# appsettings.json kontrolü
if git ls-files --error-unmatch "**/appsettings.json" &> /dev/null; then
    echo -e "${RED}❌ HATA: appsettings.json dosyası Git'e eklenmiş!${NC}"
    FOUND_SECRETS=1
else
    echo -e "${GREEN}✅ appsettings.json dosyası Git'te yok (doğru)${NC}"
fi

# appsettings.Production.json kontrolü
if git ls-files --error-unmatch "**/appsettings.Production.json" &> /dev/null; then
    echo -e "${RED}❌ HATA: appsettings.Production.json dosyası Git'e eklenmiş!${NC}"
    FOUND_SECRETS=1
else
    echo -e "${GREEN}✅ appsettings.Production.json dosyası Git'te yok (doğru)${NC}"
fi

# Git history'de secret araması
echo ""
echo "📜 Git history'de hassas bilgiler aranıyor..."

PATTERNS=(
    "password"
    "secret"
    "api_key"
    "apikey"
    "token"
    "credentials"
)

for pattern in "${PATTERNS[@]}"; do
    if git log --all --full-history -S"$pattern" -i --oneline | grep -q .; then
        echo -e "${YELLOW}⚠️  UYARI: Git history'de '$pattern' kelimesi bulundu${NC}"
    fi
done

# .gitignore kontrolü
echo ""
echo "📋 .gitignore kontrolü..."

SHOULD_IGNORE=(
    ".env"
    "appsettings.json"
    "appsettings.Production.json"
    "appsettings.Development.json"
)

for file in "${SHOULD_IGNORE[@]}"; do
    if grep -q "^$file$" .gitignore || grep -q "^\*\*/$file$" .gitignore; then
        echo -e "${GREEN}✅ $file .gitignore'da mevcut${NC}"
    else
        echo -e "${RED}❌ HATA: $file .gitignore'a eklenmemiş!${NC}"
        FOUND_SECRETS=1
    fi
done

# Sonuç
echo ""
if [ $FOUND_SECRETS -eq 0 ]; then
    echo -e "${GREEN}✅ Secret kontrolleri başarılı! Güvenli commit yapabilirsiniz.${NC}"
    exit 0
else
    echo -e "${RED}❌ Secret hataları bulundu! Lütfen düzeltin.${NC}"
    echo ""
    echo "Düzeltme adımları:"
    echo "1. Git'ten kaldır: git rm --cached <dosya>"
    echo "2. .gitignore'a ekle"
    echo "3. Git history'yi temizle (gerekirse): git filter-branch"
    exit 1
fi

