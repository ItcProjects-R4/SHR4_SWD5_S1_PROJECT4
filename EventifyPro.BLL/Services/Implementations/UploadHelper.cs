namespace EventifyPro.BLL.Services.Implementations;

public class UploadHelper : IUploadHelper
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UploadHelper(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File cannot be null or empty.", nameof(file));
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Folder name cannot be null or whitespace.", nameof(folderName));
        }

        var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDirectory = Path.Combine(webRootPath, "uploads", folderName);

        if (!Directory.Exists(uploadsDirectory))
        {
            Directory.CreateDirectory(uploadsDirectory);
        }

        var extension = Path.GetExtension(file.FileName) ?? string.Empty;
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = Path.Combine(uploadsDirectory, uniqueFileName);

        using (var fileStream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        var relativeUrl = $"/uploads/{folderName}/{uniqueFileName}";
        return relativeUrl;
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var cleanedUrl = fileUrl.TrimStart('/');
        var physicalPath = Path.GetFullPath(Path.Combine(webRootPath, cleanedUrl));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

        if (physicalPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            return false;
        }

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (string.IsNullOrEmpty(file.ContentType) || 
            !allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return false;
        }

        return true;
    }
}
