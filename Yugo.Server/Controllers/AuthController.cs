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
    private readonly AdminCredentials _adminCreds;

    public AuthController(YugoDbContext db, RemoteAppService remote, AdminCredentials adminCreds)
    {
        _db = db;
        _remote = remote;
        _adminCreds = adminCreds;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // 1. Check unique virtual admin
        if (req.Username == _adminCreds.Username && req.Password == _adminCreds.Password)
        {
            return Ok(
                new
                {
                    id = 0,
                    username = _adminCreds.Username,
                    isAdmin = true,
                }
            );
        }

        // 2. Check regular users
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || user.PasswordHash != req.Password)
            return Unauthorized("Invalid credentials");

        var userInfo = new
        {
            id = user.Id,
            username = user.Username,
            isAdmin = false,
        };
        _remote.EnqueueCommand("set_user", System.Text.Json.JsonSerializer.Serialize(userInfo));
        return Ok(userInfo);
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] int adminId)
    {
        if (adminId != 0)
            return Forbid(); // Only virtual admin allowed
        return Ok(await _db.Users.ToListAsync());
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (req.AdminId != 0)
            return Forbid();

        if (
            req.Username == _adminCreds.Username
            || await _db.Users.AnyAsync(u => u.Username == req.Username)
        )
            return BadRequest("Conflict with existing or reserved user.");

        var user = new User { Username = req.Username, PasswordHash = req.Password };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest req)
    {
        if (req.AdminId != 0)
            return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.Username = req.Username;
        if (!string.IsNullOrEmpty(req.Password))
            user.PasswordHash = req.Password;

        await _db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] int adminId)
    {
        if (adminId != 0)
            return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public record LoginRequest(string Username, string Password);

public record CreateUserRequest(string Username, string Password, int AdminId);

public record UpdateUserRequest(string Username, string Password, int AdminId);
