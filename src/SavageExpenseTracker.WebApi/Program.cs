using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Infrastructure.Data;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Infrastructure.Repositories;
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

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// Register repository
builder.Services.AddSignalR();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IChallengeRepository, ChallengeRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

// Register service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IChallengeNotificationService, ChallengeNotificationService>();
builder.Services.AddHostedService<ChallengeClosingJob>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IPhotoService, CloudinaryPhotoService>();

// Register TokenService
builder.Services.AddScoped<ITokenService, TokenService>();

// Configure JWT authentication
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                   ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                 ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not set.");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
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
        ClockSkew = TimeSpan.Zero, // Optional: Set clock skew to zero for testing purposes
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        RoleClaimType = ClaimTypes.Role // Specify the claim type for roles
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
app.UseMiddleware<SavageExpenseTracker.WebApi.Middleware.ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChallengeHub>("/challengeHub");

app.Run();
