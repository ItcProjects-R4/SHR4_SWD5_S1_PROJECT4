using Microsoft.AspNetCore.Hosting;

namespace EventifyPro.BLL.Services.Implementations;

public class UploadHelper : IUploadHelper
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UploadHelper(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();

    public void DeleteFile(string fileUrl) => 
        throw new NotImplementedException();

    public bool IsValidImage(IFormFile file) => 
        throw new NotImplementedException();
}
