namespace EventifyPro.BLL.Services.Interfaces;

public interface IUploadHelper
{
    Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    void DeleteFile(string fileUrl);
    bool IsValidImage(IFormFile file);
}
