using System.Text.Json.Serialization;

namespace FileSyncService.Models.Dto;

public sealed class FileInfoResult
{
    public string InventroyCode { get; set; } = string.Empty;
    public string FileCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreationTimeUtc { get; set; }
    public DateTime LastModifiedTimeUtc { get; set; }
    public string Md5 { get; set; } = string.Empty;
    public bool IsZipFile { get; set; }
    public List<ZipEntryInfo>? ZipEntries { get; set; }
    public bool IsArchiveFile { get; set; }
    public string ArchiveType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public List<ProcessingStep> Steps { get; set; } = [];

    public void AddStep(string step, string? detail = null)
    {
        Steps.Add(new ProcessingStep
        {
            TimestampUtc = DateTime.UtcNow,
            Step = step,
            Detail = detail
        });
    }
}

public sealed class ProcessingStep
{
    public DateTime TimestampUtc { get; set; }
    public string Step { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

public sealed class ZipEntryInfo
{
    public string EntryName { get; set; } = string.Empty;
    public long UncompressedSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public DateTime LastModifiedTimeUtc { get; set; }
    public string Md5 { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
}
