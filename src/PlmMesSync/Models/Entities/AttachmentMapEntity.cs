namespace PlmMesSync.Models.Entities;

public class AttachmentMapEntity
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int? AttachId { get; set; }
    public int? Version { get; set; }
}
