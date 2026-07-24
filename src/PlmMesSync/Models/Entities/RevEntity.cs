using System;

namespace PlmMesSync.Models.Entities;

public class RevEntity
{
    public int Id { get; set; }
    public int? Item { get; set; }
    public int? Change { get; set; }
    public string? RevNumber { get; set; }
    public string? OldRevNumber { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? LatestFlag { get; set; }
    public string? Description { get; set; }
}
