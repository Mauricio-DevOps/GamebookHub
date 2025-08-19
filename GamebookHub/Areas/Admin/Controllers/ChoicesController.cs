using GamebookHub.Data;
using GamebookHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GamebookHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Author")]
    public class ChoicesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ChoicesController(ApplicationDbContext db) => _db = db;

        // /Admin/Choices?fromNodeId=123
        public async Task<IActionResult> Index(int fromNodeId)
        {
            var node = await _db.GameNodes
                .Include(n => n.Gamebook)
                .SingleOrDefaultAsync(n => n.Id == fromNodeId);
            if (node == null) return NotFound();

            ViewBag.Node = node;
            var list = await _db.GameChoices
                .Where(c => c.FromNodeId == fromNodeId)
                .OrderBy(c => c.Id)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Create(int fromNodeId)
            => View(new GameChoice { FromNodeId = fromNodeId });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameChoice c)
        {
            // evita exigir a navegação no POST
            ModelState.Remove(nameof(GameChoice.FromNode));

            if (!ModelState.IsValid) return View(c);

            _db.GameChoices.Add(c);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { fromNodeId = c.FromNodeId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.GameChoices.FindAsync(id);
            return c == null ? NotFound() : View(c);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GameChoice c)
        {
            // evita validação da navegação (igual fizemos em Create)
            ModelState.Remove(nameof(GameChoice.FromNode));

            if (!ModelState.IsValid)
                return View(c);

            try
            {
                _db.GameChoices.Update(c);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { fromNodeId = c.FromNodeId });
            }
            catch
            {
                // fallback: mostra erro na própria tela
                ModelState.AddModelError(string.Empty, "Erro ao salvar a Choice.");
                return View(c);
            }
        }


        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.GameChoices.FindAsync(id);
            return c == null ? NotFound() : View(c);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var c = await _db.GameChoices.FindAsync(id);
            if (c == null) return NotFound();
            var from = c.FromNodeId;
            _db.GameChoices.Remove(c);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { fromNodeId = from });
        }
    }
}
