using ailab_super_app.Models;

namespace ailab_super_app.Services.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task<User> AuthenticateWithFirebaseAsync(string idToken);
        Task<User> MigrateUserToFirebaseAsync(Guid userId, string firebaseUid);
        Task<string> CreateFirebaseUserForExistingUserAsync(string email, string temporaryPassword);
    }
}
