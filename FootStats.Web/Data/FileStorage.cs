using Microsoft.AspNetCore.Components.Forms;

namespace FootStats.Web.Data;

public static class FileStorage
{
    public static async Task<string> SavePhotoAsync(IBrowserFile file, string uploadsRoot, string subfolder)
    {
        var uploadsDir = Path.Combine(uploadsRoot, subfolder);
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.Name);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        await using var target = File.Create(fullPath);
        await stream.CopyToAsync(target);

        return $"/uploads/{subfolder}/{fileName}";
    }

    public static async Task<string> SavePhotoBytesAsync(byte[] bytes, string extension, string uploadsRoot, string subfolder)
    {
        var uploadsDir = Path.Combine(uploadsRoot, subfolder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes);

        return $"/uploads/{subfolder}/{fileName}";
    }
}
