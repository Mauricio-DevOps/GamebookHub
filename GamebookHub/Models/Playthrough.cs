using System;

namespace GamebookHub.Models;

public class Playthrough
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";         // Id do AspNetUsers
    public int GamebookId { get; set; }
    public string CurrentNodeKey { get; set; } = "start";
    public string FlagsJson { get; set; } = "{}";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsFinished { get; set; }
}
