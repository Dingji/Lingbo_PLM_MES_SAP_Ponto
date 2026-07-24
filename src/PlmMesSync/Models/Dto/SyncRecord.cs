using System;

namespace PlmMesSync.Models.Dto;

public record SyncRecord(
    int Sequence,
    int BomId,
    int? ItemId,
    string? ItemNumber,
    DateTime TriggeredAt,
    DateTime ExecutedAt,
    string Status,
    string? LogFile,
    string? Error,
    string? MesStatus = null,
    string Table = "BOM");
