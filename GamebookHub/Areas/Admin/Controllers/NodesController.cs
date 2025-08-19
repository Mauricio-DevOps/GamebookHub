using GamebookHub.Data;
using GamebookHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamebookHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Author")]
    public class NodesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<NodesController> _log;

        public NodesController(ApplicationDbContext db, ILogger<NodesController> log)
        {
            _db = db;
            _log = log;
        }

        // /Admin/Nodes?gamebookId=1
        public async Task<IActionResult> Index(int gamebookId)
        {
            _log.LogInformation("GET Nodes/Index gamebookId={GamebookId}", gamebookId);
            var gb = await _db.Gamebooks.FindAsync(gamebookId);
            if (gb == null) return NotFound();
            ViewBag.Gamebook = gb;

            var nodes = await _db.GameNodes
                .Where(n => n.GamebookId == gamebookId)
                .OrderBy(n => n.Key)
                .ToListAsync();

            return View(nodes);
        }

        // GET: /Admin/Nodes/Create?gamebookId=1
        public IActionResult Create(int gamebookId)
        {
            _log.LogInformation("GET Nodes/Create gamebookId={GamebookId}", gamebookId);
            return View(new GameNode { GamebookId = gamebookId, Key = "start" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameNode model)
        {
            // evita exigir a navegação no POST
            ModelState.Remove(nameof(GameNode.Gamebook));

            if (!ModelState.IsValid) return View(model);

            _db.GameNodes.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { gamebookId = model.GamebookId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var node = await _db.GameNodes.FindAsync(id);
            return node == null ? NotFound() : View(node);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GameNode model)
        {
            ModelState.Remove(nameof(GameNode.Gamebook));

            if (!ModelState.IsValid) return View(model);

            _db.GameNodes.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { gamebookId = model.GamebookId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var node = await _db.GameNodes.FindAsync(id);
            return node == null ? NotFound() : View(node);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var node = await _db.GameNodes.FindAsync(id);
            if (node == null) return NotFound();
            var gbId = node.GamebookId;
            _db.GameNodes.Remove(node);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { gamebookId = gbId });
        }
    }
}
