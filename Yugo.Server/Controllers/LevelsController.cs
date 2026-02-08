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
            return BadRequest("XmlData required.");
        if (req.AuthorId == 0)
            return BadRequest("System admin cannot own levels.");

        var user = await _db.Users.FindAsync(req.AuthorId);
        if (user == null)
            return BadRequest("Author not found.");

        var level = new SharedLevel
        {
            Title = req.Title,
            XmlData = req.XmlData,
            AuthorId = req.AuthorId,
            Author = user,
        };
        _db.SharedLevels.Add(level);
        await _db.SaveChangesAsync();
        return Ok(level);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShareRequest req)
    {
        var level = await _db
            .SharedLevels.Include(l => l.Author)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (level == null)
            return NotFound();

        bool isGlobalAdmin = req.AuthorId == 0;
        if (level.AuthorId != req.AuthorId && !isGlobalAdmin)
            return Forbid();

        level.Title = req.Title;
        if (!string.IsNullOrEmpty(req.XmlData))
            level.XmlData = req.XmlData;

        await _db.SaveChangesAsync();
        return Ok(level);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int authorId)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();

        bool isGlobalAdmin = authorId == 0;
        if (level.AuthorId != authorId && !isGlobalAdmin)
            return Forbid();

        _db.SharedLevels.Remove(level);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/play")]
    public async Task<IActionResult> RemotePlay(int id)
    {
        var level = await _db.SharedLevels.FindAsync(id);
        if (level == null)
            return NotFound();
        _remote.EnqueueCommand("play_level", level.XmlData);
        return Ok();
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
            authorId = level.AuthorId,
        };
        _remote.EnqueueCommand("edit_level", System.Text.Json.JsonSerializer.Serialize(payload));
        return Ok();
    }

    [HttpPost("upload-file")]
    public async Task<IActionResult> UploadFile(
        [FromForm] string title,
        [FromForm] int authorId,
        IFormFile file
    )
    {
        if (authorId == 0)
            return BadRequest("System admin cannot own levels.");
        using var reader = new StreamReader(file.OpenReadStream());
        var level = new SharedLevel
        {
            Title = title,
            XmlData = await reader.ReadToEndAsync(),
            AuthorId = authorId,
        };
        _db.SharedLevels.Add(level);
        await _db.SaveChangesAsync();
        return Ok(level);
    }
}

public record ShareRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("xmlData")] string? XmlData,
    [property: JsonPropertyName("authorId")] int AuthorId
);
