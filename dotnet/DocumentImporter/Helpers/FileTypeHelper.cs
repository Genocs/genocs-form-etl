namespace Genocs.DocumentImporter.Helpers;

public class FileTypeHelper
{
    public static bool IsValidFile(string blobName)
    {
        // Check if the file is an image or a PDF
        List<string> validExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf"];
        string extension = Path.GetExtension(blobName).ToLowerInvariant();
        return validExtensions.Contains(extension);
    }
}
