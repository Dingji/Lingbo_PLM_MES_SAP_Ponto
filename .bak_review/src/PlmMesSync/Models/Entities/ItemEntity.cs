namespace PlmMesSync.Models.Entities;

public class ItemEntity
{
    public int Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultChange { get; set; }
}
