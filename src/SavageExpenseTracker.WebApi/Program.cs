using Microsoft.EntityFrameworkCore;
using SavageExpenseTracker.Infrastructure.Data;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Application.Services;
using SavageExpenseTracker.Infrastructure.Repositories;
using SavageExpenseTracker.WebApi.HostedServices;
using SavageExpenseTracker.WebApi.Services;
using SavageExpenseTracker.WebApi.Hubs;

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
builder.Services.AddSwaggerGen();

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

// Register service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IChallengeNotificationService, ChallengeNotificationService>();
builder.Services.AddHostedService<ChallengeClosingJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<ChallengeHub>("/challengeHub");

app.Run();
