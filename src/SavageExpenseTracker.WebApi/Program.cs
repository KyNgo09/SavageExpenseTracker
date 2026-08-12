using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Infrastructure.Data;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Infrastructure.Repositories;
using SavageExpenseTracker.Infrastructure.Options;
using SavageExpenseTracker.WebApi.HostedServices;
using SavageExpenseTracker.WebApi.Services;
using SavageExpenseTracker.WebApi.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using SavageExpenseTracker.Infrastructure.Services;
using Microsoft.OpenApi.Models;

var currentDir = Directory.GetCurrentDirectory();
while (currentDir != null && !File.Exists(Path.Combine(currentDir, ".env")))
{
    currentDir = Directory.GetParent(currentDir)?.FullName;
}
if (currentDir != null)
{
    DotNetEnv.Env.Load(Path.Combine(currentDir, ".env"));
}
else
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SavageExpenseTracker API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT Token in the format"
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
            Array.Empty<string>()
        }
    });
});

// Add controllers
builder.Services.AddControllers();

// Register DbContext
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? DotNetEnv.Env.GetString("DB_CONNECTION_STRING");
builder.Services.AddDbContext<SavageExpenseTrackerDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Infrastructure & Application services
builder.Services.AddSignalR();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

// Register service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IChallengeNotificationService, ChallengeNotificationService>();
builder.Services.AddHostedService<ChallengeClosingJob>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IPhotoService, CloudinaryPhotoService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<ISavageCommentQueue, SavageCommentQueue>();
builder.Services.AddHttpClient<ISavageAiService, SavageAiService>();
builder.Services.AddHostedService<SavageCommentBackgroundWorker>();

// Configure JwtOptions
builder.Services.Configure<JwtOptions>(options =>
{
    options.SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                       ?? DotNetEnv.Env.GetString("JWT_SECRET_KEY") 
                       ?? builder.Configuration["Jwt:SecretKey"] ?? string.Empty;
    options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                    ?? DotNetEnv.Env.GetString("JWT_ISSUER") 
                    ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
    options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                     ?? DotNetEnv.Env.GetString("JWT_AUDIENCE") 
                     ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
});

// Configure CloudinaryOptions
builder.Services.Configure<CloudinaryOptions>(options =>
{
    options.CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") 
                       ?? DotNetEnv.Env.GetString("CLOUDINARY_CLOUD_NAME") 
                       ?? builder.Configuration["Cloudinary:CloudName"] ?? string.Empty;
    options.ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") 
                    ?? DotNetEnv.Env.GetString("CLOUDINARY_API_KEY") 
                    ?? builder.Configuration["Cloudinary:ApiKey"] ?? string.Empty;
    options.ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") 
                       ?? DotNetEnv.Env.GetString("CLOUDINARY_API_SECRET") 
                       ?? builder.Configuration["Cloudinary:ApiSecret"] ?? string.Empty;
});

// Configure AiOptions
builder.Services.Configure<AiOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY") 
                    ?? DotNetEnv.Env.GetString("AI_API_KEY") 
                    ?? builder.Configuration["Ai:ApiKey"] ?? string.Empty;
    options.Model = Environment.GetEnvironmentVariable("AI_MODEL") 
                   ?? DotNetEnv.Env.GetString("AI_MODEL") 
                   ?? builder.Configuration["Ai:Model"] ?? string.Empty;
    options.ApiUrl = Environment.GetEnvironmentVariable("AI_API_URL") 
                    ?? DotNetEnv.Env.GetString("AI_API_URL") 
                    ?? builder.Configuration["Ai:ApiUrl"] ?? string.Empty;
    options.FallbackModels = Environment.GetEnvironmentVariable("AI_FALLBACK_MODELS") 
                            ?? DotNetEnv.Env.GetString("AI_FALLBACK_MODELS") 
                            ?? builder.Configuration["Ai:FallbackModels"] ?? string.Empty;
});

// Read JWT variables for Authentication Middleware
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                   ?? DotNetEnv.Env.GetString("JWT_SECRET_KEY") 
                   ?? builder.Configuration["Jwt:SecretKey"] 
                   ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                 ?? DotNetEnv.Env.GetString("JWT_ISSUER") 
                 ?? builder.Configuration["Jwt:Issuer"] 
                 ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not set.");

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                   ?? DotNetEnv.Env.GetString("JWT_AUDIENCE") 
                   ?? builder.Configuration["Jwt:Audience"] 
                   ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is not set.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;    
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        RoleClaimType = ClaimTypes.Role
    }; 
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<SavageExpenseTracker.WebApi.Middleware.ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChallengeHub>("/challengeHub");

app.Run();
