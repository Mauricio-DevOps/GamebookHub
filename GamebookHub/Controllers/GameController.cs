using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            .Include(g => g.CharacterSheet)
                .ThenInclude(cs => cs.Attributes)
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
            .Include(g => g.CharacterSheet)
                .ThenInclude(cs => cs.Attributes)
            .Include(g => g.Nodes)
                .ThenInclude(n => n.Choices)
            .SingleOrDefaultAsync(g => g.Slug == slug && g.IsPublished);
        if (gb == null) return NotFound();

        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var pt = await db.Playthroughs.SingleAsync(p => p.UserId == userId && p.GamebookId == gb.Id);

        var current = gb.Nodes.Single(n => n.Key == pt.CurrentNodeKey);
        var choice = current.Choices.Single(c => c.Id == choiceId);

        // Requisitos
        if (!MeetsRequirements(gb, pt.FlagsJson, choice.RequiresFlags))
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

    private static bool MeetsRequirements(Gamebook gb, string flagsJson, string? requiresJson)
    {
        if (string.IsNullOrWhiteSpace(requiresJson))
        {
            return true;
        }

        var playerFlags = ParseFlagsDict(flagsJson);
        var requirements = ParseFlagsDict(requiresJson);
        if (requirements.Count == 0)
        {
            return true;
        }

        var attributeMap = BuildAttributeMap(gb);

        foreach (var requirement in requirements)
        {
            var normalizedKey = NormalizeRequirementKey(requirement.Key);
            var lookupKey = !string.IsNullOrWhiteSpace(normalizedKey) ? normalizedKey : requirement.Key;
            if (!string.IsNullOrWhiteSpace(lookupKey) && attributeMap.TryGetValue(lookupKey, out var attributeLookup))
            {
                if (!MeetsAttributeRequirement(attributeLookup.Key, attributeLookup.Definition, requirement.Value, playerFlags))
                {
                    return false;
                }
            }
            else
            {
                if (!playerFlags.TryGetValue(lookupKey, out var storedValue) ||
                    !ValuesAreEqual(storedValue, requirement.Value))
                {
                    return false;
                }
            }
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

    private static Dictionary<string, JsonElement> ParseFlagsDict(string? input)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<string, JsonElement>(comparer);
        }

        var dict = TryParseJsonDictionary(input, comparer);
        if (dict.Count > 0)
        {
            return dict;
        }

        return ParseSimpleRequirementSyntax(input, comparer);
    }

    private static Dictionary<string, JsonElement> TryParseJsonDictionary(string input, IEqualityComparer<string> comparer)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(input);
            if (parsed != null)
            {
                return new Dictionary<string, JsonElement>(parsed, comparer);
            }
        }
        catch
        {
            // ignore and fall back to simple syntax parsing
        }

        return new Dictionary<string, JsonElement>(comparer);
    }

    private static Dictionary<string, JsonElement> ParseSimpleRequirementSyntax(string input, IEqualityComparer<string> comparer)
    {
        var dict = new Dictionary<string, JsonElement>(comparer);
        var segments = input
            .Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            segments = new[] { input };
        }

        foreach (var raw in segments)
        {
            var trimmed = raw.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var (key, value) = SplitRequirement(trimmed);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            dict[key] = CreateJsonValueElement(value);
        }

        return dict;
    }

    private static (string key, string value) SplitRequirement(string expression)
    {
        var separators = new[] { ">=", "<=", "=", ":", ">", "<" };
        foreach (var sep in separators)
        {
            var idx = expression.IndexOf(sep, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var key = expression[..idx].Trim();
                var value = expression[(idx + sep.Length)..].Trim();
                return (key, value);
            }
        }

        return (expression.Trim(), "true");
    }

    private static JsonElement CreateJsonValueElement(string valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
        {
            return JsonSerializer.SerializeToElement(true);
        }

        if (decimal.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return JsonSerializer.SerializeToElement(decimalValue);
        }

        if (bool.TryParse(valueText, out var boolValue))
        {
            return JsonSerializer.SerializeToElement(boolValue);
        }

        return JsonSerializer.SerializeToElement(valueText);
    }

    private static Dictionary<string, AttributeLookup> BuildAttributeMap(Gamebook gb)
    {
        var dict = new Dictionary<string, AttributeLookup>(StringComparer.OrdinalIgnoreCase);
        if (gb.CharacterSheet?.Attributes == null)
        {
            return dict;
        }

        foreach (var attr in gb.CharacterSheet.Attributes)
        {
            var lookup = new AttributeLookup
            {
                Definition = attr,
                Key = attr.Key ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(attr.Key))
            {
                dict[attr.Key] = lookup;
            }

            if (!string.IsNullOrWhiteSpace(attr.Label))
            {
                dict[attr.Label] = lookup;
            }
        }

        return dict;
    }

    private static bool MeetsAttributeRequirement(
        string key,
        AttributeDefinition definition,
        JsonElement requiredValue,
        Dictionary<string, JsonElement> playerFlags)
    {
        if (TryGetDecimal(requiredValue, out var requiredNumber))
        {
            if (!TryGetAttributeValue(key, definition, playerFlags, out var currentValue))
            {
                return false;
            }

            return currentValue >= requiredNumber;
        }

        if (!playerFlags.TryGetValue(key, out var storedValue))
        {
            return false;
        }

        return ValuesAreEqual(storedValue, requiredValue);
    }

    private static bool TryGetAttributeValue(
        string key,
        AttributeDefinition definition,
        Dictionary<string, JsonElement> playerFlags,
        out decimal value)
    {
        if (playerFlags.TryGetValue(key, out var storedValue) && TryGetDecimal(storedValue, out value))
        {
            return true;
        }

        if (definition.Default.HasValue)
        {
            value = definition.Default.Value;
            return true;
        }

        value = 0m;
        return false;
    }

    private static bool ValuesAreEqual(JsonElement left, JsonElement right)
    {
        if (TryGetDecimal(left, out var leftNumber) && TryGetDecimal(right, out var rightNumber))
        {
            return leftNumber == rightNumber;
        }

        var leftText = FormatValue(left);
        var rightText = FormatValue(right);
        return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => element.ToString()
        };
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDecimal(out value);
            case JsonValueKind.String:
                return decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            case JsonValueKind.True:
                value = 1m;
                return true;
            case JsonValueKind.False:
                value = 0m;
                return true;
            default:
                value = 0m;
                return false;
        }
    }

    private static string NormalizeRequirementKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        trimmed = trimmed.Trim('{', '}', '"');
        return trimmed.Trim();
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

    private sealed class AttributeLookup
    {
        public AttributeDefinition Definition { get; init; } = null!;
        public string Key { get; init; } = string.Empty;
    }
}
