namespace Presentation.Helpers;

// ************************************************************************************************
// ImageUploadHelper — saves uploaded cover images under wwwroot/uploads for event create/edit.
// ************************************************************************************************
// Consumers: `EventsController` create and update POST when `IFormFile` cover is present.
// Returns public URL path (e.g. `/uploads/events/{guid}_file.jpg`) stored on `Event.CoverImageUrl`.
// EFiles on disk; deleting events does not automatically delete uploads (orphan files possible).
// ************************************************************************************************
public static class ImageUploadHelper
{
    // ************************************************************************************************
    // UploadImageAsync — writes one image file to wwwroot and returns its web-relative URL.
    // Creates `wwwroot/uploads/{pathFolderName}/`, unique filename, copies stream.
    //  Cover hero on `EventDetails` and cards use static file middleware (`UseStaticFiles`).
    // `EventsController` passes folder `"events"` and `IWebHostEnvironment.WebRootPath`.
    // Failed upload returns null (caller should keep previous `CoverImageUrl`); disk usage grows per upload.
    // ************************************************************************************************
    public static async Task<string?> UploadImageAsync(IFormFile imageFile, string pathFolderName, IWebHostEnvironment env)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        var uploadFolder = Path.Combine(env.WebRootPath, "uploads", pathFolderName);
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return $"/uploads/{pathFolderName}/{fileName}";
    }
}
