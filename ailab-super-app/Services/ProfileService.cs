using ailab_super_app.Data;
using ailab_super_app.DTOs.User;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ailab_super_app.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        // 1. SABİTLER (CONSTANTS)
        private const string FirebaseBaseUrl = "https://firebasestorage.googleapis.com/v0/b/ailab-super-app.firebasestorage.app/o/";
        private const string FirebaseSuffix = "?alt=media";

        public ProfileService(UserManager<User> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task UpdateProfileImageAsync(Guid userId, UpdateProfileImageDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new Exception("Kullanıcı bulunamadı.");

            // Client'tan gelen URL'i doğrudan kaydediyoruz.
            // Burada bir kısıtlama yok, client Firebase'e yüklediği herhangi bir URL'i gönderebilir.
            user.ProfileImageUrl = dto.ProfileImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception("Profil fotoğrafı güncellenirken bir hata oluştu.");
            }
        }

        public DefaultAvatarListDto GetDefaultAvatars()
        {
            // 2. VARSAYILAN AVATARLAR LİSTESİ
            // Bu liste sadece kullanıcıya "Hazır Seçenekler" sunmak içindir.
            var fileNames = new List<string> { "default.webp" };

            // Man01.webp ... Man07.webp
            for (int i = 1; i <= 7; i++)
            {
                fileNames.Add($"Man{i:00}.webp");
            }

            // Woman01.webp ... Woman07.webp
            for (int i = 1; i <= 7; i++)
            {
                fileNames.Add($"Woman{i:00}.webp");
            }

            // URL Oluşturma Mantığı: Base + FileName + Suffix
            // Örnek: https://.../o/Man01.webp?alt=media
            var fullUrls = fileNames
                .Select(fileName => $"{FirebaseBaseUrl}{fileName}{FirebaseSuffix}")
                .ToList();

            return new DefaultAvatarListDto
            {
                AvatarUrls = fullUrls
            };
        }
    }
}
