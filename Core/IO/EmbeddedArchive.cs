using System.Security.Cryptography;
using System.Text;
using SevenZipExtractor;

namespace Terramon.Core.IO;

public class EmbeddedArchive
{
    private readonly string _archivePath;
    private readonly string _sha256Sum;

    public EmbeddedArchive(string archivePath, string sha256Sum)
    {
        _archivePath = archivePath;
        _sha256Sum = sha256Sum;
        OutputDirectory = Path.Combine(Terramon.CachePath, Path.GetFileNameWithoutExtension(_archivePath));
    }

    public string OutputDirectory { get; }

    public bool EnsureExtracted()
    {
        if (IsExtractedAndValid())
            return true;

        try
        {
            using var archiveStream = Terramon.Instance.GetFileStream(_archivePath);
            if (archiveStream == null)
                throw new FileNotFoundException($"Embedded archive '{_archivePath}' not found.");

            using var archiveFile = new ArchiveFile(archiveStream, SevenZipFormat.Zip);

            if (Directory.Exists(OutputDirectory))
                Directory.Delete(OutputDirectory, true);
            Directory.CreateDirectory(OutputDirectory);

            foreach (var entry in archiveFile.Entries)
            {
                var outputPath = Path.Combine(OutputDirectory, entry.FileName);
                entry.Extract(outputPath);
            }

            if (!VerifyIntegrityHash())
            {
                Directory.Delete(OutputDirectory, true);
                throw new InvalidOperationException("Extracted archive contents do not match expected integrity hash.");
            }

            Terramon.Instance.Logger.Debug($"Extracted archive {_archivePath} to {OutputDirectory}");

            return true;
        }
        catch (Exception ex)
        {
            Terramon.Instance.Logger.Error($"Failed to extract archive {_archivePath}: {ex.Message}");

            if (Directory.Exists(OutputDirectory))
                Directory.Delete(OutputDirectory, true);

            return false;
        }
    }

    public bool IsExtractedAndValid()
    {
        return Directory.Exists(OutputDirectory) && VerifyIntegrityHash();
    }

    private bool VerifyIntegrityHash()
    {
        if (string.IsNullOrEmpty(_sha256Sum))
            return true;

        using var sha256 = SHA256.Create();
        var fileHashes = new List<byte>();

        foreach (var file in Directory.EnumerateFiles(OutputDirectory, "*", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            // Get the relative path
            var relativePath = Path.GetRelativePath(OutputDirectory, file);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath.Replace('\\', '/'));
            fileHashes.AddRange(pathBytes);

            // Hash the file content
            using var stream = File.OpenRead(file);
            var hash = sha256.ComputeHash(stream);
            fileHashes.AddRange(hash);
        }

        var combinedHash = sha256.ComputeHash(fileHashes.ToArray());
        var hashString = BitConverter.ToString(combinedHash).Replace("-", "").ToLowerInvariant();
        
        //Terramon.Instance.Logger.Debug($"Calculated hash: {hashString}");

        return hashString == _sha256Sum;
    }

}