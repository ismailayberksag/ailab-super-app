using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Configuration;
using ailab_super_app.Services;
using ailab_super_app.Services.Background;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace ailab_super_app
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // PostgreSQL DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "app")
                );
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
                );
            });

            // Identity
            builder.Services.AddIdentity<User, AppRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // JwtSettings konfigürasyonu
            var jwtSection = builder.Configuration.GetSection("JwtSettings");
            builder.Services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>();
            if (string.IsNullOrEmpty(jwtSettings?.Secret))
                throw new InvalidOperationException("JwtSettings:Secret appsettings.json'da tanımlanmalıdır!");

            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            // Dual Authentication (Legacy + Firebase)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "MultiScheme";
                options.DefaultChallengeScheme = "MultiScheme";
            })
            .AddPolicyScheme("MultiScheme", "Multi Auth Scheme", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") == true)
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        // Firebase token'lar genelde çok uzundur (>500 karakter güvenli bir sınır)
                        return token.Length > 500 ? "Firebase" : "Legacy";
                    }
                    return "Legacy";
                };
            })
            .AddJwtBearer("Legacy", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings?.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddJwtBearer("Firebase", options =>
            {
                var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
                options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
                    ValidateAudience = true,
                    ValidAudience = firebaseProjectId,
                    ValidateLifetime = true
                };
            });

            // Authorization
            builder.Services.AddAuthorization();

            // Forwarded Headers (reverse proxy için)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Services
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IBugReportService, BugReportService>();
            builder.Services.AddScoped<IScoringService, ScoringService>();
            builder.Services.AddScoped<IAdminTaskService, AdminTaskService>();
            builder.Services.AddScoped<IRoomAccessService, RoomAccessService>();
            builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
            builder.Services.AddScoped<FirebaseStorageService>();
            builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>(); // Eklendi
            builder.Services.AddScoped<IReportService, ReportService>();
            
            // Background Services
            builder.Services.AddHostedService<LabAutoCheckoutWorker>();
            builder.Services.AddHostedService<DeadlinePenaltyWorker>();
            builder.Services.AddHostedService<MonthlyScoreResetWorker>();

            // Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
            });

            // Controllers
            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:3003",
                        "https://localhost:7258",
                        "http://localhost:7258",
                        "https://api.ailab.org.tr",
                        "http://192.168.5.172:5000",
                        "http://192.168.5.172:8080")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Kestrel Configuration
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(6161);
            });

            // Firebase Admin SDK Initialize
            var firebaseCredentialPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-service-account.json");

            if (!File.Exists(firebaseCredentialPath))
            {
                // Development ortamında dosya olmayabilir uyarısı yerine gereksinim gereği hata fırlatıyoruz
                throw new FileNotFoundException("Firebase service account file not found", firebaseCredentialPath);
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseCredentialPath)
            });

            var app = builder.Build();

            // Database Migration
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    logger.LogInformation("Veritabanı migration'ları kontrol ediliyor...");
                    
                    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation($"Bekleyen migration'lar bulundu: {string.Join(", ", pendingMigrations)}");
                        logger.LogInformation("Migration'lar uygulanıyor...");
                        db.Database.Migrate();
                        logger.LogInformation("Migration'lar başarıyla uygulandı!");
                    }
                    else
                    {
                        logger.LogInformation("Bekleyen migration bulunamadı, veritabanı güncel.");
                    }
                    
                    logger.LogInformation("Veritabanı hazır!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Migration sırasında hata oluştu!");
                    throw;
                }
            }

            // Middleware
            app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}