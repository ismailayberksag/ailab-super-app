#!/bin/sh

# Otomatik Migration Oluşturma Script'i
# Bu script, model değişikliklerini tespit edip otomatik migration oluşturur

echo "🔍 Model değişiklikleri kontrol ediliyor..."

# Migration'ları kontrol et
PENDING_MIGRATIONS=$(dotnet ef migrations list 2>/dev/null | grep -c "No migrations found" || echo "0")

if [ "$PENDING_MIGRATIONS" -eq 0 ]; then
    echo "✅ Mevcut migration'lar var, kontrol ediliyor..."
    
    # Pending migration'ları kontrol et
    PENDING_COUNT=$(dotnet ef migrations list 2>/dev/null | grep -c "Pending" || echo "0")
    
    if [ "$PENDING_COUNT" -gt 0 ]; then
        echo "ℹ️  Bekleyen migration'lar bulundu, runtime'da uygulanacak"
    else
        echo "✅ Veritabanı güncel, migration gerekmiyor."
    fi
else
    echo "🆕 Yeni migration'lar oluşturuluyor..."
    
    # Timestamp ile migration adı oluştur
    MIGRATION_NAME="AutoMigration_$(date +%Y%m%d_%H%M%S)"
    
    # Migration oluştur
    dotnet ef migrations add "$MIGRATION_NAME"     
    if [ $? -eq 0 ]; then
        echo "✅ Migration başarıyla oluşturuldu: $MIGRATION_NAME"
        echo "ℹ️  Migration runtime'da uygulanacak (Program.cs'de db.Database.Migrate())"
    else
        echo "❌ Migration oluşturulurken hata oluştu!"
        exit 1
    fi
fi

echo "🎉 Migration işlemi tamamlandı!"
