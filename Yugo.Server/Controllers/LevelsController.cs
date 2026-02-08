using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yugo.Server.Data;
using Yugo.Server.Models;
using Yugo.Server.Services;

namespace Yugo.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LevelsController : ControllerBase
{
    private readonly YugoDbContext _db;
    private readonly RemoteAppService _remote;

    public LevelsController(YugoDbContext db, RemoteAppService remote)
    {
        _db = db;
        _remote = remote;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        return Ok(await _db.SharedLevels.Include(l => l.Author).ToListAsync());
    }

    [HttpPost("share")]
    public async Task<IActionResult> Share([FromBody] ShareRequest req)
    {
        if (string.IsNullOrEmpty(req.XmlData))
            return BadRequest("XmlData is required for sharing.");

        var level = new SharedLevel
        {
            Title = req.Title,
            XmlData = req.XmlData,
            AuthorId = req.AuthorId,
        };
        _db.SharedLevels.Add(level);
        await _db.SaveChangesAsync();
        return Ok(level);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShareRequest req)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();
        if (level.AuthorId != req.AuthorId)
            return Forbid();

        level.Title = req.Title;
        if (!string.IsNullOrEmpty(req.XmlData))
        {
            level.XmlData = req.XmlData;
        }

        await _db.SaveChangesAsync();
        return Ok(level);
    }

    [HttpPost("upload-file")]
    public async Task<IActionResult> UploadFile(
        [FromForm] string title,
        [FromForm] int authorId,
        IFormFile file
    )
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        using var reader = new StreamReader(file.OpenReadStream());
        var xmlData = await reader.ReadToEndAsync();

        var level = new SharedLevel
        {
            Title = title,
            XmlData = xmlData,
            AuthorId = authorId,
        };
        _db.SharedLevels.Add(level);
        await _db.SaveChangesAsync();
        return Ok(level);
    }

    [HttpPost("{id}/play")]
    public async Task<IActionResult> RemotePlay(int id)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();

        _remote.EnqueueCommand("play_level", level.XmlData);
        return Ok(new { message = "Command queued for delivery" });
    }

    [HttpPost("{id}/edit")]
    public async Task<IActionResult> RemoteEdit(int id)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();

        var payload = new
        {
            data = level.XmlData,
            title = level.Title,
            cloudId = level.Id,
        };
        _remote.EnqueueCommand("edit_level", System.Text.Json.JsonSerializer.Serialize(payload));
        return Ok(new { message = "Command queued for delivery" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int authorId)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();
        if (level.AuthorId != authorId)
            return Forbid();

        _db.SharedLevels.Remove(level);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Level deleted" });
    }
}

public record ShareRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("xmlData")] string? XmlData, // Optional for updates
    [property: JsonPropertyName("authorId")] int AuthorId
);
