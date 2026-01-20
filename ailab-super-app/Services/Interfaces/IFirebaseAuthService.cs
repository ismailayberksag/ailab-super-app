using ailab_super_app.Models;
using ailab_super_app.DTOs.Auth; // Eklendi

namespace ailab_super_app.Services.Interfaces
{
    public interface IFirebaseAuthService
    {
        // Metod imzasını değiştirdik: string yerine DTO alıyor
        Task<User> AuthenticateWithFirebaseAsync(FirebaseLoginRequest request);
        
        Task<User> MigrateUserToFirebaseAsync(Guid userId, string firebaseUid);
        Task<string> CreateFirebaseUserForExistingUserAsync(string email, string temporaryPassword);
    }
}