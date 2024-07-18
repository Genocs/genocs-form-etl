using System.Collections.Generic;
using System.IO;

namespace ProcessImageEx.Helpers;

public class FileTypeHelper
{

    public static bool IsValidFile(string blobName)
    {

        // Check if the file is an image
        List<string> validExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        string extension = Path.GetExtension(blobName).ToLowerInvariant();
        return validExtensions.Contains(extension);

    }
}
