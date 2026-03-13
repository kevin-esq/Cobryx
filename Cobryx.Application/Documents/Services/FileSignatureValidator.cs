using System.IO.Compression;

using Cobryx.Domain.Shared;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Documents.Services;

/// <summary>
/// Validates file content by checking magic bytes against declared MIME type.
/// Detects MIME spoofing, rejects disallowed file types, and protects against archive bombs.
/// </summary>
public class FileSignatureValidator
{
    private readonly ILogger<FileSignatureValidator> _logger;

    /// <summary>Maximum file size in bytes (10 MB).</summary>
    public const long MaxFileSize = 10 * 1024 * 1024;

    // Archive bomb protection thresholds
    private const int MaxArchiveEntries = 100;
    private const long MaxUncompressedSize = 50 * 1024 * 1024; // 50 MB
    private const double MaxCompressionRatio = 100.0;
    private const int MaxNestingDepth = 1; // No archives inside archives

    private static readonly Dictionary<string, byte[][]> MimeSignatures = new()
    {
        ["application/pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        ["image/gif"] = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        ["application/zip"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/gif",
        "text/plain", "text/csv", "application/zip",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif",
        ".txt", ".csv", ".zip", ".docx", ".xlsx",
    };

    private static readonly HashSet<string> ZipBasedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".docx", ".xlsx",
    };

    public FileSignatureValidator(ILogger<FileSignatureValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates file size, extension whitelist, MIME whitelist, magic bytes, and archive safety.
    /// </summary>
    public Result Validate(Stream fileStream, string fileName, string declaredMimeType)
    {
        if (fileStream.Length > MaxFileSize)
            return Result.Failure(DomainErrorCode.Documents.FileSizeExceeded);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Rejected file with disallowed extension: {Extension}", extension);
            return Result.Failure(DomainErrorCode.Documents.InvalidFileType);
        }

        if (!AllowedMimeTypes.Contains(declaredMimeType))
        {
            _logger.LogWarning("Rejected file with disallowed MIME type: {MimeType}", declaredMimeType);
            return Result.Failure(DomainErrorCode.Documents.InvalidFileType);
        }

        if (MimeSignatures.TryGetValue(declaredMimeType, out var signatures))
        {
            var maxLen = signatures.Max(s => s.Length);
            var header = new byte[maxLen];
            fileStream.Position = 0;
            var bytesRead = fileStream.Read(header, 0, maxLen);
            fileStream.Position = 0;

            if (bytesRead < maxLen)
            {
                _logger.LogWarning("File too small to validate magic bytes for {MimeType}", declaredMimeType);
                return Result.Failure(DomainErrorCode.Documents.InvalidFileType);
            }

            if (!signatures.Any(sig => header.Take(sig.Length).SequenceEqual(sig)))
            {
                _logger.LogWarning("Magic byte mismatch: declared {MimeType}", declaredMimeType);
                return Result.Failure(DomainErrorCode.Documents.InvalidFileType);
            }
        }

        // Archive bomb protection for ZIP-based files
        if (ZipBasedExtensions.Contains(extension))
        {
            var archiveResult = ValidateArchiveSafety(fileStream, fileName);
            if (!archiveResult.IsSuccess)
                return archiveResult;
        }

        return Result.Success();
    }

    /// <summary>
    /// Inspects a ZIP-based file for decompression bombs:
    /// excessive entries, high compression ratio, oversized uncompressed content, and nested archives.
    /// </summary>
    private Result ValidateArchiveSafety(Stream fileStream, string fileName)
    {
        fileStream.Position = 0;

        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

            if (archive.Entries.Count > MaxArchiveEntries)
            {
                _logger.LogWarning("Archive bomb: {FileName} contains {Count} entries (max {Max})",
                    fileName, archive.Entries.Count, MaxArchiveEntries);
                return Result.Failure(DomainErrorCode.Documents.SuspiciousArchive);
            }

            long totalUncompressed = 0;
            var nestedArchiveCount = 0;

            foreach (var entry in archive.Entries)
            {
                totalUncompressed += entry.Length;

                // Check total uncompressed size
                if (totalUncompressed > MaxUncompressedSize)
                {
                    _logger.LogWarning("Archive bomb: {FileName} total uncompressed size exceeds {MaxMB}MB",
                        fileName, MaxUncompressedSize / (1024 * 1024));
                    return Result.Failure(DomainErrorCode.Documents.SuspiciousArchive);
                }

                // Check compression ratio per entry
                if (entry.CompressedLength > 0)
                {
                    var ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > MaxCompressionRatio)
                    {
                        _logger.LogWarning("Archive bomb: {FileName} entry {Entry} has compression ratio {Ratio:F1}:1 (max {Max}:1)",
                            fileName, entry.FullName, ratio, MaxCompressionRatio);
                        return Result.Failure(DomainErrorCode.Documents.SuspiciousArchive);
                    }
                }

                // Detect nested archives
                var entryExt = Path.GetExtension(entry.FullName);
                if (ZipBasedExtensions.Contains(entryExt))
                {
                    nestedArchiveCount++;
                    if (nestedArchiveCount > MaxNestingDepth)
                    {
                        _logger.LogWarning("Archive bomb: {FileName} contains {Count} nested archives (max {Max})",
                            fileName, nestedArchiveCount, MaxNestingDepth);
                        return Result.Failure(DomainErrorCode.Documents.SuspiciousArchive);
                    }
                }
            }

            fileStream.Position = 0;
            return Result.Success();
        }
        catch (InvalidDataException)
        {
            _logger.LogWarning("Corrupt or invalid archive: {FileName}", fileName);
            return Result.Failure(DomainErrorCode.Documents.InvalidFileType);
        }
    }
}

