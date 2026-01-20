namespace ailab_super_app.DTOs.Auth
{
    public class FirebaseLoginRequest
    {
        public string IdToken { get; set; } = default!;
    }

    public class CreateFirebaseUserRequest
    {
        public string Email { get; set; } = default!;
        public string TemporaryPassword { get; set; } = default!; // Min 6 karakter olmalı
    }
}
