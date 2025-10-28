using ailab_super_app.Data;
using ailab_super_app.Models;
using ailab_super_app.Configuration;
using ailab_super_app.Services;
using ailab_super_app.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
//deneme commit

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
                // Pending model changes warning'ini suppress et (production için)
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

            // JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
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
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IRoomAccessService, RoomAccessService>();
            builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

            // Controllers
            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // CORS (Frontend için - gerekirse ayarla)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:3003",
                        "https://localhost:7258",
                        "http://localhost:7258",
                        "https://api.ailab.org.tr")  // Sadece belirli domainler
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Kestrel'i 0.0.0.0 üzerinden dinlemesi için yapılandır (reverse proxy arkasında)
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(6161); // Sadece HTTP (CloudPanel/Nginx SSL sonlandırma yapacak)
            });

            var app = builder.Build();

            // Database migration'ı otomatik çalıştır
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    logger.LogInformation("Veritabanı migration'ları kontrol ediliyor...");
                    
                    // Pending migration'ları kontrol et
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
            app.UseForwardedHeaders(); // Reverse proxy için (X-Forwarded-For, X-Forwarded-Proto)

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

            // HTTPS redirection CloudPanel/Nginx tarafından yapılacak
            // app.UseHttpsRedirection(); // Reverse proxy arkasında gereksiz

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}