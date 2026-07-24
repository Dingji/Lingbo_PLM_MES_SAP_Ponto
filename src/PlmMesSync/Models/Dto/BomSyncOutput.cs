using System;
using System.Collections.Generic;

namespace PlmMesSync.Models.Dto;

public class BomSyncOutput
{
    public string ParentItemNumber { get; set; } = string.Empty;
    public string? ParentDescription { get; set; }
    public List<RevVersionOutput> Versions { get; set; } = new();
}

public class RevVersionOutput
{
    public string RevNumber { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public bool IsLatest { get; set; }
    public List<BomLineOutput> Lines { get; set; } = new();
}

public class BomLineOutput
{
    public int Level { get; set; } = 1;
    public int FindNumber { get; set; }
    public string? ItemNumber { get; set; }
    public string? Quantity { get; set; }
    public string? Description { get; set; }
    public string RefDesig { get; set; } = string.Empty;
    public string? SubstituteGroup { get; set; }
    public string? SubstitutePriority { get; set; }
    public string? ChangeInNumber { get; set; }
    public string? ChangeOutNumber { get; set; }
    public string? ComponentRev { get; set; }
    public string? ComponentRevDescription { get; set; }
    public bool HasChildren { get; set; }
}
