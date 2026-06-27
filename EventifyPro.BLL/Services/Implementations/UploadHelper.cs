namespace EventifyPro.BLL.Services.Implementations;

public class UploadHelper : IUploadHelper
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ISystemSettingService _systemSettingService;

    public UploadHelper(IWebHostEnvironment webHostEnvironment, ISystemSettingService systemSettingService)
    {
        _webHostEnvironment = webHostEnvironment;
        _systemSettingService = systemSettingService;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File cannot be null or empty.", nameof(file));
        }

        // Enforce 5MB size limit
        const long maxSizeBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            throw new InvalidOperationException("File size exceeds the maximum limit of 5 MB.");
        }

        // Reject SVG files to prevent XSS vulnerability
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SVG file uploads are blocked for security reasons (XSS prevention).");
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Folder name cannot be null or whitespace.", nameof(folderName));
        }

        // Validate file extension against AllowedUploadExtensions setting
        var allowedExtensionsString = await _systemSettingService.GetSettingValueAsync("AllowedUploadExtensions", ".jpg,.jpeg,.png,.pdf", cancellationToken);
        var allowedExtensions = allowedExtensionsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().ToLowerInvariant())
            .ToList();

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed types are: {allowedExtensionsString}");
        }

        var contentRootPath = _webHostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();
        var uploadsDirectory = Path.Combine(contentRootPath, "uploads", folderName);

        if (!Directory.Exists(uploadsDirectory))
        {
            Directory.CreateDirectory(uploadsDirectory);
        }

        string uniqueFileName;
        string physicalPath;

        if (IsValidImage(file))
        {
            uniqueFileName = $"{Guid.NewGuid()}.webp";
            physicalPath = Path.Combine(uploadsDirectory, uniqueFileName);

            using (var inputStream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(inputStream, cancellationToken))
            {
                int? targetWidth = null;
                int? targetHeight = null;
                var resizeMode = ResizeMode.Max;

                if (string.Equals(folderName, "organizers", StringComparison.OrdinalIgnoreCase))
                {
                    targetWidth = 256;
                    targetHeight = 256;
                    resizeMode = ResizeMode.Crop;
                }
                else if (string.Equals(folderName, "events", StringComparison.OrdinalIgnoreCase))
                {
                    targetWidth = 1200;
                    targetHeight = 675;
                    resizeMode = ResizeMode.Max;
                }

                if (targetWidth.HasValue && targetHeight.HasValue)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(targetWidth.Value, targetHeight.Value),
                        Mode = resizeMode
                    }));
                }

                await image.SaveAsWebpAsync(physicalPath, new WebpEncoder
                {
                    Quality = 80
                }, cancellationToken);
            }
        }
        else
        {
            // Image folders should strictly accept only valid images
            if (string.Equals(folderName, "organizers", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "events", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "profiles", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only valid image files (.jpg, .jpeg, .png, .webp, .gif) are allowed for this folder.");
            }

            var fileExt = Path.GetExtension(file.FileName) ?? string.Empty;
            uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
            physicalPath = Path.Combine(uploadsDirectory, uniqueFileName);

            using (var fileStream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }
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

        var contentRootPath = _webHostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();
        var cleanedUrl = fileUrl.TrimStart('/');
        var physicalPath = Path.GetFullPath(Path.Combine(contentRootPath, cleanedUrl));
        var uploadsRoot = Path.GetFullPath(Path.Combine(contentRootPath, "uploads"));

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
