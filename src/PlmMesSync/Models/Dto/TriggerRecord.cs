using System;

namespace PlmMesSync.Models.Dto;

public record TriggerRecord(
    int Sequence,
    int BomId,
    string Action,
    string Table,
    DateTime ReceivedAt);
