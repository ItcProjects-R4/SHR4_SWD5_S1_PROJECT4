using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventifyPro.BLL.Services.Interfaces;

/// <summary>
/// Service abstraction for validating and uploading images.
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Validates and uploads an image file.
    /// </summary>
    /// <param name="file">The image file to validate and upload.</param>
    /// <param name="folderName">The upload folder name (e.g. "organizers", "events", "profiles").</param>
    /// <param name="maxSizeBytes">Optional max file size limit. Defaults to 2MB for profile/organizers, 5MB for events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the relative image URL if successful; otherwise, validation failure errors.</returns>
    Task<Result<string>> UploadImageAsync(
        IFormFile? file, 
        string folderName, 
        long? maxSizeBytes = null, 
        CancellationToken cancellationToken = default);
}
