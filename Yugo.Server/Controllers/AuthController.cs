using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yugo.Server.Data;
using Yugo.Server.Models;
using Yugo.Server.Services;

namespace Yugo.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly YugoDbContext _db;
    private readonly RemoteAppService _remote;

    public AuthController(YugoDbContext db, RemoteAppService remote)
    {
        _db = db;
        _remote = remote;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || user.PasswordHash != req.Password) // Simplified for prototype
            return Unauthorized("Invalid credentials");

        var userInfo = new
        {
            id = user.Id,
            username = user.Username,
            isAdmin = user.IsAdmin,
        };

        // Queue command for retry if App is not yet connected
        _remote.EnqueueCommand("set_user", System.Text.Json.JsonSerializer.Serialize(userInfo));

        return Ok(userInfo);
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        // In a real app, check if current user is admin
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return BadRequest("User already exists");

        var user = new User
        {
            Username = req.Username,
            PasswordHash = req.Password,
            IsAdmin = req.IsAdmin,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User created" });
    }
}

public record LoginRequest(string Username, string Password);

public record CreateUserRequest(string Username, string Password, bool IsAdmin);
