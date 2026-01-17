using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ailab_super_app.Services
{
    public class FirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "ailab-super-app.appspot.com"; // Bu bucket adı varsayılan, configden alınmalı aslında.
        // Not: Gerçek bucket adını bilmiyorsak firebase-service-account.json'dan okuyamayız, genelde project-id.appspot.com olur.
        // Şimdilik sabit varsayalım veya kullanıcıya soralım. Ancak kod içinde configden okumak en iyisi.

        public FirebaseStorageService(IConfiguration configuration)
        {
            var credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-service-account.json");
            
            // Credential yükle
            var credential = GoogleCredential.FromFile(credentialPath);
            _storageClient = StorageClient.Create(credential);
            
            // Bucket name configden gelebilir
             _bucketName = configuration["Firebase:StorageBucket"] ?? "ailab-super-app.firebasestorage.app";
        }

        public async Task<string> UploadFileAsync(IFormFile file, string destinationPath)
        {
            using var stream = file.OpenReadStream();
            var obj = await _storageClient.UploadObjectAsync(_bucketName, destinationPath, file.ContentType, stream);
            
            // Public URL veya Signed URL döndürebiliriz.
            // Firebase Storage için genelde:
            // https://firebasestorage.googleapis.com/v0/b/[bucket]/o/[path]?alt=media
            
            // Ancak Signed URL daha güvenli (Download için)
            // Şimdilik storage path'i döndürelim, download ederken signed url üretiriz.
            return destinationPath; 
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(_bucketName, filePath);
            }
            catch (Exception ex)
            {
                // Dosya zaten yoksa hata fırlatmayabiliriz veya loglarız
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        public async Task<string> GetSignedUrlAsync(string filePath, int expirationMinutes = 60)
        {
            var urlSigner = UrlSigner.FromCredentialFile(Path.Combine(Directory.GetCurrentDirectory(), "firebase-service-account.json"));
            return await urlSigner.SignAsync(_bucketName, filePath, TimeSpan.FromMinutes(expirationMinutes), HttpMethod.Get);
        }
    }
}