using GamebookHub.Data;
using GamebookHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GamebookHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Author")]
    public class GamebooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GamebooksController> _log;

        public GamebooksController(ApplicationDbContext context, ApplicationDbContext db, ILogger<GamebooksController> log)
        {
            _context = context;
            _db = db; _log = log;
        }

        // GET: Admin/Gamebooks
        public async Task<IActionResult> Index()
        {
            return View(await _context.Gamebooks.ToListAsync());
        }

        // GET: Admin/Gamebooks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var gamebook = await _context.Gamebooks
                .Include(g => g.CharacterSheet)
                    .ThenInclude(cs => cs.Attributes)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gamebook == null) return NotFound();

            return View(gamebook);
        }
        //So para teste

        // GET: Admin/Gamebooks/Create
        public IActionResult Create()
        {
            // opcional: inicializar estruturas para evitar nulls na View
            // return View(new Gamebook { CharacterSheet = new CharacterSheetTemplate { Inventory = new InventoryConfig() } });
            return View();
        }

        // POST: Admin/Gamebooks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Slug,Description,CoverUrl,AuthorId,PublishedAt,IsPublished,CharacterSheet")] Gamebook gamebook)
        {
            // Normalização + validações server-side específicas da ficha
            NormalizeAndValidateCharacterSheet(gamebook);

            if (!ModelState.IsValid)
            {
                // Retorna a View com os dados preenchidos e mensagens de erro
                return View(gamebook);
            }

            _context.Add(gamebook);
            await _context.SaveChangesAsync();

            var attrs = gamebook.CharacterSheet?.Attributes;
            var count = attrs?.Count;  // deve ser > 0


            return RedirectToAction(nameof(Index));
        }

        // --------------------------
        // Helpers
        // --------------------------
        private void NormalizeAndValidateCharacterSheet(Gamebook gamebook)
        {
            var cs = gamebook.CharacterSheet;
            if (cs == null || !cs.Enabled)
            {
                // Se ficha está desativada, não persistimos dados de ficha
                gamebook.CharacterSheet = null;
                return;
            }

            // Garantir estruturas
            cs.Attributes ??= new System.Collections.Generic.List<AttributeDefinition>();
            cs.Inventory ??= new InventoryConfig();

            // Remover atributos "vazios" (sem label e key)
            cs.Attributes = cs.Attributes
                .Where(a => !string.IsNullOrWhiteSpace(a?.Label) || !string.IsNullOrWhiteSpace(a?.Key))
                .ToList();

            // Slugify da Key quando ausente e validação de unicidade (case-insensitive)
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < cs.Attributes.Count; i++)
            {
                var a = cs.Attributes[i];
                if (a == null) continue;

                a.Key = string.IsNullOrWhiteSpace(a.Key) ? Slugify(a.Label) : Slugify(a.Key);
                a.Label = a.Label?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(a.Key))
                    ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Key", "Key obrigatória.");

                if (!seen.Add(a.Key ?? string.Empty))
                    ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Key", "Key duplicada neste gamebook.");

                // Validações por tipo
                switch (a.Type)
                {
                    case AttributeType.Integer:
                    case AttributeType.Decimal:
                    case AttributeType.Resource:
                        // min/max/default coerentes quando informados
                        if (a.Min.HasValue && a.Max.HasValue && a.Min > a.Max)
                            ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Min", "Min não pode ser maior que Max.");

                        if (a.Default.HasValue)
                        {
                            if (a.Min.HasValue && a.Default < a.Min)
                                ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Default", "Default abaixo do Min.");
                            if (a.Max.HasValue && a.Default > a.Max)
                                ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Default", "Default acima do Max.");
                        }

                        if (a.Type == AttributeType.Resource)
                        {
                            // Recurso: por convenção Min >= 0 e Max obrigatório
                            if (!a.Max.HasValue)
                                ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Max", "Resource requer Max.");
                            if (a.Min.HasValue && a.Min.Value < 0)
                                ModelState.AddModelError($"CharacterSheet.Attributes[{i}].Min", "Resource requer Min >= 0.");
                        }
                        break;

                    case AttributeType.Enum:
                        if (string.IsNullOrWhiteSpace(a.EnumOptions) || a.EnumOptions.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Count() < 2)
                            ModelState.AddModelError($"CharacterSheet.Attributes[{i}].EnumOptions", "Enum requer pelo menos 2 opções.");
                        break;

                    case AttributeType.Boolean:
                    case AttributeType.Text:
                    default:
                        // Sem validações adicionais aqui
                        break;
                }
            }

            // Inventário
            if (cs.Inventory.Enabled)
            {
                switch (cs.Inventory.Mode)
                {
                    case InventoryMode.Slots:
                        if (!cs.Inventory.Slots.HasValue || cs.Inventory.Slots.Value < 1)
                            ModelState.AddModelError("CharacterSheet.Inventory.Slots", "Informe um número de slots >= 1.");
                        cs.Inventory.Capacity = null; // não usado neste modo
                        break;
                    case InventoryMode.Weight:
                        if (!cs.Inventory.Capacity.HasValue || cs.Inventory.Capacity.Value <= 0)
                            ModelState.AddModelError("CharacterSheet.Inventory.Capacity", "Informe uma capacidade > 0.");
                        cs.Inventory.Slots = null; // não usado neste modo
                        break;
                    case InventoryMode.Unlimited:
                    default:
                        cs.Inventory.Slots = null;
                        cs.Inventory.Capacity = null;
                        break;
                }
            }
            else
            {
                // Inventário desativado: zera configs
                cs.Inventory.Enabled = false;
                cs.Inventory.Mode = InventoryMode.Unlimited;
                cs.Inventory.Slots = null;
                cs.Inventory.Capacity = null;
            }
        }

        private static string Slugify(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToLowerInvariant();
            s = s.Normalize(System.Text.NormalizationForm.FormD);
            var arr = s.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            s = new string(arr);
            s = System.Text.RegularExpressions.Regex.Replace(s, "[^a-z0-9]+", "-");
            s = System.Text.RegularExpressions.Regex.Replace(s, "(^-|-$)", string.Empty);
            return s;
        }

        // GET: Admin/Gamebooks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var gamebook = await _context.Gamebooks
                .Include(g => g.CharacterSheet)
                    .ThenInclude(cs => cs.Attributes)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gamebook == null) return NotFound();

            // evita nulls na View
            if (gamebook.CharacterSheet == null)
                gamebook.CharacterSheet = new CharacterSheetTemplate { Inventory = new InventoryConfig() };
            else if (gamebook.CharacterSheet.Inventory == null)
                gamebook.CharacterSheet.Inventory = new InventoryConfig();

            return View(gamebook);
        }

        // POST: Admin/Gamebooks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Slug,Description,CoverUrl,AuthorId,PublishedAt,IsPublished,CharacterSheet")] Gamebook gamebook)
        {
            if (id != gamebook.Id) return NotFound();

            // Normaliza slug (usa Title se não vier)
            var slugSource = string.IsNullOrWhiteSpace(gamebook.Slug) ? gamebook.Title : gamebook.Slug;
            gamebook.Slug = Slugify(slugSource);

            // Slug único (desconsiderando o próprio Id)
            if (await _context.Set<Gamebook>().AnyAsync(g => g.Slug == gamebook.Slug && g.Id != id))
                ModelState.AddModelError(nameof(Gamebook.Slug), "Já existe um gamebook com esse slug. Escolha outro.");

            // Valida ficha/inventário
            NormalizeAndValidateCharacterSheet(gamebook);

            if (!ModelState.IsValid) return View(gamebook);

            // Carrega do banco com relacionamentos
            var db = await _context.Gamebooks
                .Include(g => g.CharacterSheet)
                    .ThenInclude(cs => cs.Attributes)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (db == null) return NotFound();

            // Campos simples
            db.Title = gamebook.Title;
            db.Slug = gamebook.Slug;
            db.Description = gamebook.Description;
            db.CoverUrl = gamebook.CoverUrl;
            db.AuthorId = gamebook.AuthorId;
            db.PublishedAt = gamebook.PublishedAt;
            db.IsPublished = gamebook.IsPublished;

            // Ficha
            if (gamebook.CharacterSheet == null || !gamebook.CharacterSheet.Enabled)
            {
                // remover ficha existente
                if (db.CharacterSheet != null)
                {
                    _context.RemoveRange(db.CharacterSheet.Attributes);
                    _context.Remove(db.CharacterSheet);
                    db.CharacterSheet = null;
                }
            }
            else
            {
                db.CharacterSheet ??= new CharacterSheetTemplate();
                db.CharacterSheet.Enabled = true;

                // Inventário
                db.CharacterSheet.Inventory ??= new InventoryConfig();
                db.CharacterSheet.Inventory.Enabled = gamebook.CharacterSheet.Inventory.Enabled;
                db.CharacterSheet.Inventory.Mode = gamebook.CharacterSheet.Inventory.Mode;
                db.CharacterSheet.Inventory.Slots = gamebook.CharacterSheet.Inventory.Slots;
                db.CharacterSheet.Inventory.Capacity = gamebook.CharacterSheet.Inventory.Capacity;

                // Sincroniza atributos (add/update/remove)
                var incoming = gamebook.CharacterSheet.Attributes ?? new List<AttributeDefinition>();

                var incomingIds = new HashSet<int>(incoming.Where(a => a.Id > 0).Select(a => a.Id));
                var toRemove = db.CharacterSheet.Attributes.Where(a => a.Id > 0 && !incomingIds.Contains(a.Id)).ToList();
                foreach (var rem in toRemove) _context.Remove(rem);

                foreach (var a in incoming)
                {
                    if (a.Id > 0)
                    {
                        var ex = db.CharacterSheet.Attributes.FirstOrDefault(x => x.Id == a.Id);
                        if (ex != null)
                        {
                            ex.Key = Slugify(a.Key);
                            ex.Label = a.Label?.Trim() ?? "";
                            ex.Type = a.Type;
                            ex.Min = a.Min;
                            ex.Max = a.Max;
                            ex.Default = a.Default;
                            ex.Visible = a.Visible;
                            ex.Order = a.Order;
                            ex.EnumOptions = a.EnumOptions;
                        }
                    }
                    else
                    {
                        db.CharacterSheet.Attributes.Add(new AttributeDefinition
                        {
                            Key = Slugify(a.Key),
                            Label = a.Label?.Trim() ?? "",
                            Type = a.Type,
                            Min = a.Min,
                            Max = a.Max,
                            Default = a.Default,
                            Visible = a.Visible,
                            Order = a.Order,
                            EnumOptions = a.EnumOptions
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Admin/Gamebooks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gamebook = await _context.Gamebooks
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gamebook == null)
            {
                return NotFound();
            }

            return View(gamebook);
        }

        // POST: Admin/Gamebooks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gamebook = await _context.Gamebooks.FindAsync(id);
            if (gamebook != null)
            {
                _context.Gamebooks.Remove(gamebook);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GamebookExists(int id)
        {
            return _context.Gamebooks.Any(e => e.Id == id);
        }

        // GET: /Admin/Gamebooks/Import
        public IActionResult Import() => View();

        // POST: /Admin/Gamebooks/Import
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(string json, bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                ModelState.AddModelError("", "Cole o JSON.");
                return View();
            }

            GamebookImportDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<GamebookImportDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("JSON inválido.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao ler JSON: {ex.Message}");
                return View();
            }

            if (string.IsNullOrWhiteSpace(dto.Slug))
            {
                ModelState.AddModelError("", "O campo 'slug' é obrigatório.");
                return View();
            }
            if (!dto.Nodes.Any(n => n.Key == "start"))
            {
                ModelState.AddModelError("", "Precisa existir um nó com Key = 'start'.");
                return View();
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            // upsert do gamebook
            var gb = await _db.Gamebooks
                .Include(g => g.Nodes).ThenInclude(n => n.Choices)
                .SingleOrDefaultAsync(g => g.Slug == dto.Slug);

            if (gb == null)
            {
                gb = new Gamebook
                {
                    Title = dto.Title,
                    Slug = dto.Slug,
                    Description = dto.Description ?? "",
                    CoverUrl = dto.CoverUrl,
                    IsPublished = dto.IsPublished
                };
                _db.Gamebooks.Add(gb);
                await _db.SaveChangesAsync();
            }
            else
            {
                gb.Title = dto.Title;
                gb.Description = dto.Description ?? "";
                gb.CoverUrl = dto.CoverUrl;
                gb.IsPublished = dto.IsPublished;
                _db.Gamebooks.Update(gb);

                if (overwrite)
                {
                    // apaga nós/choices existentes
                    _db.GameChoices.RemoveRange(gb.Nodes.SelectMany(n => n.Choices));
                    _db.GameNodes.RemoveRange(gb.Nodes);
                }
            }

            await _db.SaveChangesAsync();

            // recria nós e choices
            var keyToNode = new Dictionary<string, GameNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in dto.Nodes)
            {
                var node = new GameNode
                {
                    GamebookId = gb.Id,
                    Key = n.Key,
                    Text = n.Text,
                    IsEnding = n.IsEnding
                };
                _db.GameNodes.Add(node);
                keyToNode[n.Key] = node;
            }
            await _db.SaveChangesAsync();

            // choices (já temos os Ids dos nós)
            foreach (var n in dto.Nodes)
            {
                if (!keyToNode.TryGetValue(n.Key, out var from)) continue;
                foreach (var c in n.Choices)
                {
                    _db.GameChoices.Add(new GameChoice
                    {
                        FromNodeId = from.Id,
                        Label = c.Label,
                        ToNodeKey = c.ToNodeKey,
                        RequiresFlags = c.RequiresFlags,
                        SetsFlags = c.SetsFlags
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["ok"] = $"Importado: {gb.Title}";
            return RedirectToAction(nameof(Index));
        }

    }
}
