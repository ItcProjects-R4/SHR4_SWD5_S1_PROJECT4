
namespace EventifyPro.Controllers;

[Route("uploads")]
public class UploadsController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UploadsController(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("{folder}/{filename}")]
    public IActionResult ServeFile(string folder, string filename)
    {
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(filename))
        {
            return BadRequest("Invalid folder or filename.");
        }

        // Sanitize the inputs to prevent Path Traversal
        folder = Path.GetFileName(folder);
        filename = Path.GetFileName(filename);

        var contentRootPath = _webHostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();
        var filePath = Path.Combine(contentRootPath, "uploads", folder, filename);

        // Verify the file actually exists
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        // Verify path stays within the uploads folder under content root
        var uploadsRoot = Path.GetFullPath(Path.Combine(contentRootPath, "uploads"));
        var absoluteFilePath = Path.GetFullPath(filePath);
        if (!absoluteFilePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Access denied.");
        }

        // Apply resource-based authorization logic
        if (string.Equals(folder, "profiles", StringComparison.OrdinalIgnoreCase))
        {
            // strictly require authentication for user profiles
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
        }

        var contentType = GetContentType(filePath);
        return PhysicalFile(filePath, contentType);
    }

    private string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
