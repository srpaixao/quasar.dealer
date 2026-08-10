using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using QuasarApi.DataBase;
using QuasarApi.Services.Interfaces;
using QuasarApi.Services;

namespace QuasarApi.Extensions
{
    public static class BuilderExtensions
    {
        public static void AddArchitectures(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSwaggerGen();

            // Database
            builder.Services.AddDbContext<AppDbContext>(options => options
                .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // CORS
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                throw new ArgumentNullException("AllowedOrigins", "A chave AllowedOrigins não está configurada.");
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    //builder.WithOrigins(allowedOrigins)
                    //       .AllowAnyMethod()
                    //       .AllowAnyHeader()
                    //       .AllowCredentials();

                    builder.AllowAnyOrigin() // Permite todas as origens                         
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                });


            });


            // JWT
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException("Jwt:Key", "A chave JWT não está configurada.");
            }

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("token"))
                        {
                            context.Token = context.Request.Cookies["token"];
                        }
                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                };
            });


            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

        }

        public static void AddServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IAreaService, AreaService>();
            builder.Services.AddScoped<IVolumeService, VolumeService>();
            builder.Services.AddScoped<IConferenciaVolumeService, ConferenciaVolumeService>();
        }
    }
}
