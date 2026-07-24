using System.Collections.Generic;

namespace PlmMesSync.Models.Entities;

public class BomEntity
{
    public int Id { get; set; }
    public int? Item { get; set; }
    public string? ItemNumber { get; set; }
    public string? FindNumber { get; set; }
    public string? Quantity { get; set; }
    public string? Description { get; set; }
    public int? ChangeIn { get; set; }
    public int? ChangeOut { get; set; }
    public int? Component { get; set; }
    public int? List06 { get; set; }
    public int? List07 { get; set; }

    public ItemEntity? ParentItem { get; set; }
    public ICollection<RefDesigEntity> RefDesigs { get; set; } = new List<RefDesigEntity>();
}
