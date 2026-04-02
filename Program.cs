using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MovieRentalApp.Contexts;
using MovieRentalApp.Interfaces;
using MovieRentalApp.Middleware;
using MovieRentalApp.Models;
using MovieRentalApp.Repositories;
using MovieRentalApp.Services;
using System.Text;

namespace MovieRentalApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Database ──────────────────────────────────────────
            builder.Services.AddDbContext<MovieContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Development")));

            builder.Services.AddMemoryCache();

            // ── Repositories ──────────────────────────────────────
            builder.Services.AddScoped<IRepository<int, User>, Repository<int, User>>();
            builder.Services.AddScoped<IRepository<int, Movie>, Repository<int, Movie>>();
            builder.Services.AddScoped<IRepository<int, Rental>, Repository<int, Rental>>();
            builder.Services.AddScoped<IRepository<int, Payment>, Repository<int, Payment>>();
            builder.Services.AddScoped<IRepository<int, Wishlist>, Repository<int, Wishlist>>();
            builder.Services.AddScoped<IRepository<int, Genre>, Repository<int, Genre>>();
            builder.Services.AddScoped<IRepository<int, MovieGenre>, Repository<int, MovieGenre>>();
            builder.Services.AddScoped<IRepository<int, AuditLog>, Repository<int, AuditLog>>();
            builder.Services.AddScoped<IRepository<int, Notification>, Repository<int, Notification>>();
            builder.Services.AddScoped<IRepository<int, Cart>, Repository<int, Cart>>();
            builder.Services.AddScoped<IRepository<int, MovieRating>, Repository<int, MovieRating>>();

            // ── Audit Log Service ──────────────────────────────────
            builder.Services.AddScoped<AuditLogService>();

            // ── Business Services ──────────────────────────────────
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IMovieService, MovieService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRentalService, RentalService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IGenreService, GenreService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IMovieRatingService, MovieRatingService>();

            // ── Chatbot Service ────────────────────────────────────
            builder.Services.AddHttpClient<IChatbotService, ChatbotService>(client =>
            {
                var baseUrl = builder.Configuration["PythonAI:BaseUrl"] ?? "http://localhost:8000";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ── CORS ───────────────────────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // ── JWT ────────────────────────────────────────────────
            var jwtKey = builder.Configuration["Keys:Jwt"] ?? builder.Configuration["keys:Jwt"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is missing from appsettings.json.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = "MovieRentalApp",
                        ValidAudience            = "MovieRentalApp",
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey))
                    };

                    // Return proper JSON on 401/403 instead of blank response
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async ctx =>
                        {
                            // Suppress default redirect/challenge
                            ctx.HandleResponse();

                            if (ctx.Response.HasStarted) return;

                            ctx.Response.StatusCode  = 401;
                            ctx.Response.ContentType = "application/json";

                            var reason = ctx.AuthenticateFailure?.Message;
                            var message = string.IsNullOrEmpty(reason)
                                ? "Authentication required. Please provide a valid token."
                                : reason.Contains("Lifetime")
                                    ? "Token has expired. Please log in again."
                                    : "Invalid token. Please log in again.";

                            await ctx.Response.WriteAsync(
                                System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    statusCode = 401,
                                    message
                                }));
                        },
                        OnForbidden = async ctx =>
                        {
                            ctx.Response.StatusCode  = 403;
                            ctx.Response.ContentType = "application/json";
                            await ctx.Response.WriteAsync(
                                System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    statusCode = 403,
                                    message = "You do not have permission to access this resource."
                                }));
                        }
                    };
                });

            // ── Rate Limiting ──────────────────────────────────────
            builder.Services.Configure<IpRateLimitOptions>(
                builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // ── MVC / Swagger ──────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Movie Rental API",
                    Version = "v1"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // ── Build App ──────────────────────────────────────────
            var app = builder.Build();

            // ── 1. Global Exception Handler ────────────────────────
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // ── 2. CORS ────────────────────────────────────────────
            app.UseCors();

            // ── 3. Swagger ─────────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie Rental API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            // ── 4. HTTPS Redirect ──────────────────────────────────
            app.UseHttpsRedirection();

            // ── 5. Static Files ────────────────────────────────────
            var mimeProvider = new FileExtensionContentTypeProvider();
            mimeProvider.Mappings[".mp4"] = "video/mp4";
            mimeProvider.Mappings[".webm"] = "video/webm";
            mimeProvider.Mappings[".ogg"] = "video/ogg";
            mimeProvider.Mappings[".mkv"] = "video/x-matroska";
            mimeProvider.Mappings[".avi"] = "video/x-msvideo";
            mimeProvider.Mappings[".m3u8"] = "application/x-mpegURL";
            mimeProvider.Mappings[".mov"] = "video/quicktime";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = mimeProvider,
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=3600";
                }
            });

            // ── 6. Auth & Routing ──────────────────────────────────
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIpRateLimiting();
            app.MapControllers();
            app.Run();
        }
    }
}