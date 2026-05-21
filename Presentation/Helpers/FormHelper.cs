namespace Presentation.Helpers;

public static class FormHelper
{
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
