using System.Text.Json;
using GamebookHub.Data;
using GamebookHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamebookHub.Controllers;

[Authorize] // exige login para jogar
public class GameController(ApplicationDbContext db) : Controller
{
    [HttpGet("/Game/{slug}")]
    public async Task<IActionResult> Play(string slug)
    {
        var gb = await db.Gamebooks
            .Include(g => g.Nodes)
            .ThenInclude(n => n.Choices)
            .SingleOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
        if (gb == null) return NotFound();

        var userId = User?.Identity?.Name ?? User?.FindFirst("sub")?.Value ?? User?.FindFirst("nameidentifier")?.Value;
        // Como estamos usando Identity padrão, recupera o Id real:
        userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? userId!;

        var pt = await db.Playthroughs.SingleOrDefaultAsync(p => p.UserId == userId && p.GamebookId == gb.Id);
        if (pt == null)
        {
            pt = new Playthrough
            {
                UserId = userId,
                GamebookId = gb.Id,
                CurrentNodeKey = "start",
                FlagsJson = "{}"              // <<< importante
            };
            db.Playthroughs.Add(pt);
            await db.SaveChangesAsync();
        }

        var node = gb.Nodes.Single(n => n.Key == pt.CurrentNodeKey);
        return View("Node", (gb, node, pt));
    }

    [HttpPost("/Game/{slug}/choose")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Choose(string slug, int choiceId)
    {
        var gb = await db.Gamebooks
            .Include(g => g.Nodes)
            .ThenInclude(n => n.Choices)
            .SingleOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
        if (gb == null) return NotFound();

        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var pt = await db.Playthroughs.SingleAsync(p => p.UserId == userId && p.GamebookId == gb.Id);

        var current = gb.Nodes.Single(n => n.Key == pt.CurrentNodeKey);
        var choice = current.Choices.Single(c => c.Id == choiceId);

        // Requisitos
        if (!MeetsRequirements(pt.FlagsJson, choice.RequiresFlags))
            return RedirectToAction("Play", new { slug });

        // Atualiza flags e avança
        pt.FlagsJson = ApplyFlags(pt.FlagsJson, choice.SetsFlags);
        pt.CurrentNodeKey = choice.ToNodeKey;
        pt.UpdatedAt = DateTime.UtcNow;
        var next = gb.Nodes.Single(n => n.Key == pt.CurrentNodeKey);
        if (next.IsEnding) pt.IsFinished = true;

        await db.SaveChangesAsync();
        return RedirectToAction("Play", new { slug });
    }

    private static bool MeetsRequirements(string flagsJson, string? requiresJson)
    {
        if (string.IsNullOrWhiteSpace(requiresJson)) return true;
        var have = JsonSerializer.Deserialize<Dictionary<string, object>>(flagsJson) ?? new();
        var need = JsonSerializer.Deserialize<Dictionary<string, object>>(requiresJson) ?? new();
        foreach (var kv in need)
        {
            if (!have.TryGetValue(kv.Key, out var v)) return false;
            if (!Equals(v?.ToString(), kv.Value?.ToString())) return false;
        }
        return true;
    }

    private static string ApplyFlags(string flagsJson, string? setsJson)
    {
        if (string.IsNullOrWhiteSpace(setsJson)) return flagsJson;
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(flagsJson) ?? new();
        var set = JsonSerializer.Deserialize<Dictionary<string, object>>(setsJson) ?? new();
        foreach (var kv in set) dict[kv.Key] = kv.Value!;
        return JsonSerializer.Serialize(dict);
    }

    [HttpPost("/Game/{slug}/restart")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restart(string slug)
    {
        var gb = await db.Gamebooks.SingleOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
        if (gb == null) return NotFound();

        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var pt = await db.Playthroughs.SingleOrDefaultAsync(p => p.UserId == userId && p.GamebookId == gb.Id);

        if (pt == null)
        {
            pt = new Playthrough { UserId = userId, GamebookId = gb.Id, CurrentNodeKey = "start", FlagsJson = "{}", IsFinished = false, UpdatedAt = DateTime.UtcNow };
            db.Playthroughs.Add(pt);
        }
        else
        {
            pt.CurrentNodeKey = "start";
            pt.FlagsJson = "{}";
            pt.IsFinished = false;
            pt.UpdatedAt = DateTime.UtcNow;
            db.Playthroughs.Update(pt);
        }

        await db.SaveChangesAsync();
        return RedirectToAction("Play", new { slug });
    }


}
