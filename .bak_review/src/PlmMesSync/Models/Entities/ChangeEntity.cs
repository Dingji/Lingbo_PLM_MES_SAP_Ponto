using System;

namespace PlmMesSync.Models.Entities;

public class ChangeEntity
{
    public int Id { get; set; }
    public string? ChangeNumber { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
