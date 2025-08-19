// Models/GameNode.cs
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GamebookHub.Models;

public class GameNode
{
    public int Id { get; set; }
    public int GamebookId { get; set; }

    [ValidateNever]
    public Gamebook? Gamebook { get; set; }  // <-- era não nulo; torne ? e ignore validação

    public string Key { get; set; } = "start";
    public string Text { get; set; } = "";
    public bool IsEnding { get; set; }

    public List<GameChoice> Choices { get; set; } = new();
}
