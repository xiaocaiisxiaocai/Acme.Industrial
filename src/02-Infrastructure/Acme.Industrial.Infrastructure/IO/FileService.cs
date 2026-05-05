using Acme.Industrial.Core.Serialization;

namespace Acme.Industrial.Infrastructure.IO;

/// <summary>
/// 文件操作选项。
/// </summary>
public class FileOptions
{
    public bool CreateDirectoryIfNotExists { get; set; } = true;
    public bool Overwrite { get; set; } = true;
    public System.Text.Encoding? Encoding { get; set; }
    public int BufferSize { get; set; } = 4096;
}

/// <summary>
/// 文件服务实现。
/// </summary>
public class FileService
{
    private readonly ISerializer _serializer;

    public FileService(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public async Task<string> ReadAllTextAsync(string path, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        return await File.ReadAllTextAsync(path, options.Encoding ?? System.Text.Encoding.UTF8);
    }

    public async Task WriteAllTextAsync(string path, string content, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        await File.WriteAllTextAsync(path, content, options.Encoding ?? System.Text.Encoding.UTF8);
    }

    public async Task<byte[]> ReadAllBytesAsync(string path, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        return await File.ReadAllBytesAsync(path);
    }

    public async Task WriteAllBytesAsync(string path, byte[] data, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        await File.WriteAllBytesAsync(path, data);
    }

    public async Task<T?> ReadJsonAsync<T>(string path, FileOptions? options = null)
    {
        var json = await ReadAllTextAsync(path, options);
        return _serializer.Deserialize<T>(json);
    }

    public async Task WriteJsonAsync<T>(string path, T data, FileOptions? options = null)
    {
        options ??= new FileOptions();
        var json = _serializer.Serialize(data);
        await WriteAllTextAsync(path, json, options);
    }

    public async Task<IReadOnlyList<string>> ReadAllLinesAsync(string path, FileOptions? options = null)
    {
        var content = await ReadAllTextAsync(path, options);
        return content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    public async Task WriteAllLinesAsync(string path, IEnumerable<string> lines, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        var content = string.Join(Environment.NewLine, lines);
        await File.WriteAllTextAsync(path, content, options.Encoding ?? System.Text.Encoding.UTF8);
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public FileInfo GetInfo(string path)
    {
        return new FileInfo(path);
    }

    public string GetExtension(string path)
    {
        return Path.GetExtension(path);
    }

    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public string GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path) ?? string.Empty;
    }

    public string Combine(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public async Task<Stream> OpenReadAsync(string path)
    {
        return File.OpenRead(path);
    }

    public async Task<Stream> OpenWriteAsync(string path, FileOptions? options = null)
    {
        options ??= new FileOptions();
        EnsureDirectoryExists(path, options);

        var mode = options.Overwrite ? FileMode.Create : FileMode.CreateNew;
        return File.Open(path, mode, FileAccess.Write, FileShare.Read);
    }

    public async Task CopyAsync(string sourcePath, string destPath, bool overwrite = true)
    {
        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        File.Copy(sourcePath, destPath, overwrite);
    }

    public async Task MoveAsync(string sourcePath, string destPath)
    {
        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        File.Move(sourcePath, destPath);
    }

    private static void EnsureDirectoryExists(string path, FileOptions options)
    {
        if (!options.CreateDirectoryIfNotExists) return;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

/// <summary>
/// 目录服务实现。
/// </summary>
public class DirectoryService
{
    public bool Exists(string path)
    {
        return Directory.Exists(path);
    }

    public void Create(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void Delete(string path, bool recursive = false)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }
    }

    public IReadOnlyList<string> GetFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return Array.Empty<string>();
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(path, searchPattern, option);
    }

    public IReadOnlyList<string> GetDirectories(string path, string searchPattern = "*", bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return Array.Empty<string>();
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetDirectories(path, searchPattern, option);
    }

    public long GetSize(string path, bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, "*", option);

        return files.Sum(f => new FileInfo(f).Length);
    }

    public DateTime GetLastWriteTime(string path)
    {
        return Directory.Exists(path)
            ? Directory.GetLastWriteTime(path)
            : DateTime.MinValue;
    }

    public void Move(string sourcePath, string destPath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, destPath);
        }
    }

    public void Copy(string sourcePath, string destPath, bool recursive = true)
    {
        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        if (!Directory.Exists(destPath))
        {
            Directory.CreateDirectory(destPath);
        }

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var destFile = Path.Combine(destPath, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        if (recursive)
        {
            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                var destDir = Path.Combine(destPath, Path.GetFileName(dir));
                Copy(dir, destDir, true);
            }
        }
    }
}

/// <summary>
/// 临时文件服务。
/// </summary>
public class TempFileService : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirectories = new();
    private bool _disposed;

    public string CreateTempFile(string? extension = null)
    {
        var tempFile = Path.GetTempFileName();
        if (!string.IsNullOrEmpty(extension))
        {
            var newPath = tempFile + extension;
            File.Move(tempFile, newPath);
            tempFile = newPath;
        }
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    public string GetTempFileName(string prefix = "", string? extension = null)
    {
        var fileName = $"{prefix}{Guid.NewGuid():N}";
        if (!string.IsNullOrEmpty(extension))
        {
            fileName += extension;
        }
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    public void Cleanup()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try { File.Delete(file); } catch { }
        }

        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(dir, true); } catch { }
        }

        _tempFiles.Clear();
        _tempDirectories.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cleanup();
    }
}
