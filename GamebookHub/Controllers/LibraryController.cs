using GamebookHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamebookHub.Controllers;

public class LibraryController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = db.Gamebooks.AsNoTracking().Where(g => g.IsPublished);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(g => g.Title.Contains(q));
        var list = await query
            .OrderByDescending(g => g.PublishedAt)
            .Select(g => new { g.Slug, g.Title, g.Description, g.CoverUrl })
            .ToListAsync();

        ViewData["q"] = q;
        return View(list);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var gb = await db.Gamebooks.AsNoTracking()
            .SingleOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
        if (gb == null) return NotFound();
        return View(gb);
    }
}
