namespace GestorRHPro.Services;

public sealed class EmployeeDocumentStorage(IWebHostEnvironment environment, IConfiguration configuration)
{
    private string PrivateDirectory => configuration["Storage:EmployeeDocumentsPath"]
        ?? Path.Combine(environment.ContentRootPath, "App_Data", "EmployeeDocuments");

    public async Task<string> SaveAsync(IFormFile file, string extension, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(PrivateDirectory);
        var storageKey = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(PrivateDirectory, storageKey);
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);
            return storageKey;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public string? ResolvePath(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;

        var normalized = storageKey.Replace('\\', '/');
        var isLegacyUpload = normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        var path = isLegacyUpload
            ? Path.Combine(environment.WebRootPath, "uploads", fileName)
            : Path.Combine(PrivateDirectory, fileName);
        return File.Exists(path) ? path : null;
    }

    public void Delete(string storageKey)
    {
        var path = ResolvePath(storageKey);
        if (path is not null) File.Delete(path);
    }
}
