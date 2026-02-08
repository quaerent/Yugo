using Microsoft.EntityFrameworkCore;
using Yugo.Server.Data;
using Yugo.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Define Admin Credentials record
builder.Services.AddSingleton(provider =>
{
    string user = "admin";
    string pass = "admin";
    var configPath = Path.Combine(AppContext.BaseDirectory, "admin_config.json");
    if (!File.Exists(configPath))
        configPath = Path.Combine(Directory.GetCurrentDirectory(), "admin_config.json");

    if (File.Exists(configPath))
    {
        try
        {
            var json = File.ReadAllText(configPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            user = doc.RootElement.GetProperty("username").GetString() ?? user;
            pass = doc.RootElement.GetProperty("password").GetString() ?? pass;
        }
        catch { }
    }
    return new AdminCredentials(user, pass);
});

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbType = Environment.GetEnvironmentVariable("DB_TYPE") ?? "SQLITE";

builder.Services.AddDbContext<YugoDbContext>(options =>
{
    if (dbType == "POSTGRES")
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString ?? "Data Source=yugo.db");
});

builder.Services.AddSingleton<RemoteAppService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YugoDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public record AdminCredentials(string Username, string Password);
