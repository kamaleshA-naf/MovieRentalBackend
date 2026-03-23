using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
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

            builder.Services.AddDbContext<MovieContext>(options =>
                options.UseSqlServer(
                    builder.Configuration
                           .GetConnectionString("Development")));

            builder.Services.AddMemoryCache();

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


            

            #region CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            #endregion

            var jwtKey = builder.Configuration["Keys:Jwt"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException(
                    "JWT Key is missing from appsettings.json.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                        
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = "MovieRentalApp",
                            ValidAudience = "MovieRentalApp",
                            IssuerSigningKey =  new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                        };
                });

            builder.Services.Configure<IpRateLimitOptions>(
                builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration,
                RateLimitConfiguration>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Movie Rental API",
                    Version = "v1"
                });
                options.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter your JWT token."
                    });
                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id   = "Bearer"
                                }
                            },
                            new string[] {}
                        }
                    });
            });

            var app = builder.Build();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint(
                        "/swagger/v1/swagger.json",
                        "Movie Rental API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            
            var mimeProvider = new FileExtensionContentTypeProvider();
            mimeProvider.Mappings[".mp4"] = "video/mp4";
            mimeProvider.Mappings[".webm"] = "video/webm";
            mimeProvider.Mappings[".ogg"] = "video/ogg";
            mimeProvider.Mappings[".mkv"] = "video/x-matroska";
            mimeProvider.Mappings[".avi"] = "video/x-msvideo";
            mimeProvider.Mappings[".m3u8"] = "application/x-mpegURL";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = mimeProvider,
                OnPrepareResponse = ctx =>
                {
                   
                    ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                }
            });

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIpRateLimiting();
            app.MapControllers();
            app.Run();
        }
    }
}