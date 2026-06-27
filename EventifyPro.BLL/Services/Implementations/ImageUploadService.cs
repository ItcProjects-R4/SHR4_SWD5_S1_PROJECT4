namespace EventifyPro.BLL.Services.Implementations;

/// <summary>
/// Service for validating and uploading images, eliminating controller-level duplication.
/// </summary>
public class ImageUploadService : IImageUploadService
{
    private readonly IUploadHelper _uploadHelper;

    public ImageUploadService(IUploadHelper uploadHelper)
    {
        _uploadHelper = uploadHelper;
    }

    public async Task<Result<string>> UploadImageAsync(
        IFormFile? file, 
        string folderName, 
        long? maxSizeBytes = null, 
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return Result<string>.Failure("Please upload a file.");
        }

        // Determine size limit
        var sizeLimit = maxSizeBytes ?? (string.Equals(folderName, "events", StringComparison.OrdinalIgnoreCase) 
            ? 5 * 1024 * 1024 
            : 2 * 1024 * 1024);

        if (file.Length > sizeLimit)
        {
            var limitMb = sizeLimit / (1024 * 1024);
            return Result<string>.Failure($"Image size must not exceed {limitMb}MB.");
        }

        if (!_uploadHelper.IsValidImage(file))
        {
            return Result<string>.Failure("Please upload a valid image file (.jpg, .jpeg, .png, .webp).");
        }

        try
        {
            var url = await _uploadHelper.UploadFileAsync(file, folderName, cancellationToken);
            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}
