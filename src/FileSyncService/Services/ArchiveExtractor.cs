using SharpCompress.Archives;
using SharpCompress.Readers;

using System.Security.Cryptography;

namespace FileSyncService.Services;

/// <summary>
/// A single leaf file produced by recursively extracting an archive down to its deepest level.
/// </summary>
public sealed class ExtractedFile
{
    public string Name { get; set; } = string.Empty;
    public string EntryPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public byte[] Bytes { get; set; } = [];
    public DateTime LastModifiedUtc { get; set; }
    public string Md5 { get; set; } = string.Empty;
}

/// <summary>
/// Recursively extracts archives (including archives nested inside archives) down to the
/// deepest level, returning only the non-archive leaf files.
/// </summary>
public static class ArchiveExtractor
{
    private const int MaxDepth = 16;

    private static readonly string[] ArchiveExtensions =
        [".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".xz", ".tar.gz", ".tar.bz2", ".tar.xz"];

    /// <summary>
    /// Determines whether a file name looks like a supported archive based on its extension.
    /// </summary>
    public static bool IsArchiveName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var lower = name.ToLowerInvariant();
        return ArchiveExtensions.Any(lower.EndsWith);
    }

    /// <summary>
    /// Opens the archive at <paramref name="archivePath"/> and recursively collects all leaf files.
    /// </summary>
    public static List<ExtractedFile> ExtractLeaves(string archivePath, CancellationToken cancellationToken)
    {
        var leaves = new List<ExtractedFile>();

        using var fs = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920);
        using var archive = ArchiveFactory.OpenArchive(fs, new ReaderOptions());
        CollectFromArchive(archive, prefix: "", leaves, depth: 0, cancellationToken);

        return leaves;
    }

    private static void CollectFromArchive(
        IArchive archive, string prefix, List<ExtractedFile> leaves, int depth, CancellationToken cancellationToken)
    {
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.IsDirectory)
                continue;

            var entryKey = entry.Key ?? "";
            var name = Path.GetFileName(entryKey);
            var displayPath = string.IsNullOrEmpty(prefix) ? entryKey : $"{prefix}/{name}";

            byte[] bytes;
            using (var entryStream = entry.OpenEntryStream())
            using (var ms = new MemoryStream())
            {
                entryStream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            // Recurse into nested archives until the deepest level is reached.
            if (bytes.Length > 0 && depth < MaxDepth && IsArchiveName(name))
            {
                try
                {
                    using var inner = new MemoryStream(bytes);
                    using var innerArchive = ArchiveFactory.OpenArchive(inner, new ReaderOptions());
                    CollectFromArchive(innerArchive, displayPath, leaves, depth + 1, cancellationToken);
                    continue;
                }
                catch
                {
                    // The entry has an archive extension but cannot be opened; treat it as a normal leaf.
                }
            }

            leaves.Add(new ExtractedFile
            {
                Name = name,
                EntryPath = displayPath,
                Size = bytes.Length,
                Bytes = bytes,
                LastModifiedUtc = entry.LastModifiedTime ?? DateTime.MinValue,
                Md5 = bytes.Length > 0 ? Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant() : string.Empty
            });
        }
    }
}
