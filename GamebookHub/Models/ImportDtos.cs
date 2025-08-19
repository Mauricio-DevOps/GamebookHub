namespace GamebookHub.Models;

public class GamebookImportDto
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public List<NodeImportDto> Nodes { get; set; } = new();
}

public class NodeImportDto
{
    public string Key { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsEnding { get; set; }
    public List<ChoiceImportDto> Choices { get; set; } = new();
}

public class ChoiceImportDto
{
    public string Label { get; set; } = "";
    public string ToNodeKey { get; set; } = "";
    public string? RequiresFlags { get; set; }
    public string? SetsFlags { get; set; }
}
