using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using ailab_super_app.Models.Enums;
using ailab_super_app.Helpers;

namespace ailab_super_app.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(
            AppDbContext context,
            UserManager<User> userManager,
            ILogger<FirebaseAuthService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<User> AuthenticateWithFirebaseAsync(string idToken)
        {
            try
            {
                // 1. Firebase token'ı doğrula
                var decodedToken = await FirebaseAuth.DefaultInstance
                    .VerifyIdTokenAsync(idToken);

                var firebaseUid = decodedToken.Uid;
                var email = decodedToken.Claims.ContainsKey("email")
                    ? decodedToken.Claims["email"].ToString()
                    : null;

                if (string.IsNullOrEmpty(email))
                {
                    throw new UnauthorizedAccessException("Email is required in Firebase token");
                }

                // 2. Bu Firebase UID ile kullanıcı var mı kontrol et
                var user = await _context.Users
                    .IgnoreQueryFilters() // Silinmiş kullanıcıları da kontrol et (belki geri dönmüştür?) - İsteğe bağlı
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);

                if (user != null)
                {
                    if (user.IsDeleted) throw new Exception("Bu hesap silinmiş.");
                    
                    _logger.LogInformation("User authenticated with Firebase UID: {FirebaseUid}", firebaseUid);
                    return user; // Zaten Firebase'e geçmiş kullanıcı
                }

                // 3. Email ile kullanıcı var mı kontrol et (Migration case)
                // UserManager default olarak silinmişleri getirmez (eğer filter varsa)
                user = await _userManager.FindByEmailAsync(email);
                
                if (user != null)
                {
                    if (user.IsDeleted) throw new Exception("Bu hesap silinmiş.");

                    if (user.AuthProvider == AuthProvider.Legacy)
                    {
                        // Legacy kullanıcıyı Firebase'e otomatik migrate et
                        _logger.LogInformation("Auto-migrating user to Firebase: {Email}", email);
                        return await MigrateUserToFirebaseAsync(user.Id, firebaseUid);
                    }
                    else if (user.AuthProvider == AuthProvider.Firebase)
                    {
                        // Kullanıcı Firebase provider ama UID set edilmemiş (Veri tutarsızlığı düzeltme)
                        _logger.LogWarning("Fixing missing FirebaseUid for user: {Email}", email);
                        user.FirebaseUid = firebaseUid;
                        await _context.SaveChangesAsync();
                        return user;
                    }
                }

                // 4. Yeni kullanıcı - Firebase'den otomatik kayıt
                _logger.LogInformation("Creating new user from Firebase: {Email}", email);
                
                var now = DateTimeHelper.GetTurkeyTime();
                user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = email, // Username email olarak başlar, kullanıcı sonra değiştirebilir
                    Email = email,
                    EmailConfirmed = decodedToken.Claims.ContainsKey("email_verified")
                        && (bool)decodedToken.Claims["email_verified"],
                    FirebaseUid = firebaseUid,
                    AuthProvider = AuthProvider.Firebase,
                    Status = UserStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ProfileImageUrl = "https://firebasestorage.googleapis.com/v0/b/ailab-super-app.firebasestorage.app/o/default.webp?alt=media"
                };

                // Şifresiz kullanıcı oluştur
                var result = await _userManager.CreateAsync(user);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user: {errors}");
                }

                // Varsayılan rol ata
                await _userManager.AddToRoleAsync(user, "Member");

                return user;
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Firebase authentication failed");
                throw new UnauthorizedAccessException("Invalid Firebase token", ex);
            }
        }

        public async Task<User> MigrateUserToFirebaseAsync(Guid userId, string firebaseUid)
        {
            var user = await _context.Users
                .Include(u => u.RfidCards)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new Exception($"User not found: {userId}");
            }

            // KRİTİK: Sadece auth bilgileri değişiyor, diğer veriler KORUNUYOR
            user.FirebaseUid = firebaseUid;
            user.AuthProvider = AuthProvider.Firebase;
            user.MigratedToFirebaseAt = DateTimeHelper.GetTurkeyTime();

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User migrated to Firebase. UserId: {UserId}, Email: {Email}, RFID Cards: {CardCount}",
                user.Id, user.Email, user.RfidCards?.Count ?? 0);

            return user;
        }

        public async Task<string> CreateFirebaseUserForExistingUserAsync(string email, string temporaryPassword)
        {
            try
            {
                // Firebase'de kullanıcı oluştur
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = email,
                    Password = temporaryPassword,
                    EmailVerified = false
                });

                // Password reset linki oluştur
                var resetLink = await FirebaseAuth.DefaultInstance
                    .GeneratePasswordResetLinkAsync(email);

                _logger.LogInformation("Firebase user created: {Email}, UID: {Uid}", email, userRecord.Uid);

                return resetLink;
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to create Firebase user for: {Email}", email);
                throw new Exception($"Firebase user creation failed: {ex.Message}", ex);
            }
        }
    }
}
