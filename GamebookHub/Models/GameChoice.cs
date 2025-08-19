using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GamebookHub.Models;

public class GameChoice
{
    public int Id { get; set; }

    public int FromNodeId { get; set; }

    [ValidateNever]
    public GameNode? FromNode { get; set; }  // <- torne anulável e ignore validação

    public string Label { get; set; } = "";
    public string ToNodeKey { get; set; } = "";

    public string? RequiresFlags { get; set; }
    public string? SetsFlags { get; set; }
}
