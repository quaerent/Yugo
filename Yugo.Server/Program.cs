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

// Database
builder.Services.AddDbContext<YugoDbContext>(options => options.UseSqlite("Data Source=yugo.db"));

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

// Ensure database created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YugoDbContext>();
    db.Database.EnsureCreated();

    var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (adminUser == null)
    {
        db.Users.Add(
            new Yugo.Server.Models.User
            {
                Username = "admin",
                PasswordHash = "admin",
                IsAdmin = true,
            }
        );
        await db.SaveChangesAsync();
    }
}

app.Run();
