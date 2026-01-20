using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Services.Interfaces;
using ailab_super_app.Models.Enums;
using ailab_super_app.Helpers;
using ailab_super_app.DTOs.Auth; // Eklendi

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

        public async Task<User> AuthenticateWithFirebaseAsync(FirebaseLoginRequest request)
        {
            try
            {
                // 1. Firebase token'ı doğrula
                var decodedToken = await FirebaseAuth.DefaultInstance
                    .VerifyIdTokenAsync(request.IdToken);

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
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);

                if (user != null)
                {
                    if (user.IsDeleted) throw new Exception("Bu hesap silinmiş.");
                    
                    _logger.LogInformation("User authenticated with Firebase UID: {FirebaseUid}", firebaseUid);
                    return user;
                }

                // 3. Email ile kullanıcı var mı kontrol et (Migration case)
                user = await _userManager.FindByEmailAsync(email);
                
                if (user != null)
                {
                    if (user.IsDeleted) throw new Exception("Bu hesap silinmiş.");

                    if (user.AuthProvider == AuthProvider.Legacy)
                    {
                        _logger.LogInformation("Auto-migrating user to Firebase: {Email}", email);
                        return await MigrateUserToFirebaseAsync(user.Id, firebaseUid);
                    }
                    else if (user.AuthProvider == AuthProvider.Firebase)
                    {
                        _logger.LogWarning("Fixing missing FirebaseUid for user: {Email}", email);
                        user.FirebaseUid = firebaseUid;
                        await _context.SaveChangesAsync();
                        return user;
                    }
                }

                // 4. Yeni kullanıcı - Firebase'den otomatik kayıt
                // BURADA EK BİLGİLERİ KULLANIYORUZ
                _logger.LogInformation("Creating new user from Firebase: {Email}", email);

                // Validasyon: Yeni kayıt için zorunlu alanlar
                if (string.IsNullOrEmpty(request.FullName))
                {
                    // Frontend'e not: Register ise FullName gönderilmeli
                    throw new Exception("Yeni kayıt için Ad Soyad (FullName) zorunludur.");
                }
                
                var now = DateTimeHelper.GetTurkeyTime();
                user = new User
                {
                    Id = Guid.NewGuid(),
                    // UserName varsa onu, yoksa email'i kullan
                    UserName = !string.IsNullOrEmpty(request.UserName) ? request.UserName : email,
                    Email = email,
                    EmailConfirmed = decodedToken.Claims.ContainsKey("email_verified")
                        && (bool)decodedToken.Claims["email_verified"],
                    FirebaseUid = firebaseUid,
                    AuthProvider = AuthProvider.Firebase,
                    Status = UserStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ProfileImageUrl = "https://firebasestorage.googleapis.com/v0/b/ailab-super-app.firebasestorage.app/o/default.webp?alt=media",
                    
                    // EK ALANLAR
                    FullName = request.FullName,
                    SchoolNumber = request.SchoolNumber,
                    PhoneNumber = request.PhoneNumber,
                    Phone = request.PhoneNumber // Alias
                };

                // UserName benzersizlik kontrolü
                if (await _userManager.FindByNameAsync(user.UserName) != null)
                {
                     throw new Exception($"Kullanıcı adı '{user.UserName}' zaten kullanılıyor.");
                }

                var result = await _userManager.CreateAsync(user);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user: {errors}");
                }

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
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = email,
                    Password = temporaryPassword,
                    EmailVerified = false
                });

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