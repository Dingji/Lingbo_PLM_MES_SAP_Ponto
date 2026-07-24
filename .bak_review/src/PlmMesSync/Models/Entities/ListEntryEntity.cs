namespace PlmMesSync.Models.Entities;

public class ListEntryEntity
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int? Active { get; set; }
    public int? EntryId { get; set; }
    public string? EntryValue { get; set; }
    public int? LangId { get; set; }
    public string? Description { get; set; }
}
