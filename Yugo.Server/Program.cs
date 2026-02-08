using Microsoft.EntityFrameworkCore;
using Yugo.Server.Data;
using Yugo.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database logic: Use Postgres if in Docker/Production, otherwise SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbType = Environment.GetEnvironmentVariable("DB_TYPE") ?? "SQLITE";

builder.Services.AddDbContext<YugoDbContext>(options =>
{
    if (dbType == "POSTGRES")
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=yugo.db");
    }
});

// Custom Services
builder.Services.AddSingleton<RemoteAppService>();

// CORS for Vite
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Ensure database created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YugoDbContext>();
    db.Database.EnsureCreated();

    // External Admin Config
    string adminUserStr = "admin";
    string adminPassStr = "admin";

    var configPath = Path.Combine(AppContext.BaseDirectory, "admin_config.json");
    if (File.Exists(configPath))
    {
        try
        {
            var configJson = File.ReadAllText(configPath);
            var configDoc = System.Text.Json.JsonDocument.Parse(configJson);
            adminUserStr =
                configDoc.RootElement.GetProperty("username").GetString() ?? adminUserStr;
            adminPassStr =
                configDoc.RootElement.GetProperty("password").GetString() ?? adminPassStr;
            Console.WriteLine(
                $"[Auth] Loaded external admin configuration for user: {adminUserStr}"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Auth] Failed to load external admin config: {ex.Message}");
        }
    }

    var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == adminUserStr);
    if (adminUser == null)
    {
        db.Users.Add(
            new Yugo.Server.Models.User
            {
                Username = adminUserStr,
                PasswordHash = adminPassStr,
                IsAdmin = true,
            }
        );
        await db.SaveChangesAsync();
    }
    else
    {
        // Update password if config changed
        if (adminUser.PasswordHash != adminPassStr)
        {
            adminUser.PasswordHash = adminPassStr;
            await db.SaveChangesAsync();
            Console.WriteLine("[Auth] Admin password updated from config file.");
        }
    }
}

app.Run();
