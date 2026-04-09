namespace SecondHandShop.WebApi.Utilities;

/// <summary>
/// Validates admin preview uploads using file signatures (not client-reported Content-Type).
/// </summary>
internal static class AdminPreviewImageValidation
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    /// <summary>
    /// Strips directory segments and checks extension is allowlisted.
    /// </summary>
    public static bool TryGetSafeFileName(string? originalFileName, out string safeFileName, out string extension)
    {
        safeFileName = string.Empty;
        extension = string.Empty;
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return false;
        }

        var name = Path.GetFileName(originalFileName.Trim());
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        extension = Path.GetExtension(name);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return false;
        }

        safeFileName = name;
        return true;
    }

    /// <summary>
    /// Returns true when <paramref name="extension"/> is consistent with the detected image format.
    /// </summary>
    public static bool ExtensionMatchesSignature(string extension, string signatureExtension)
    {
        if (string.Equals(extension, signatureExtension, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // JPEG: .jpg and .jpeg both acceptable for image/jpeg
        if (signatureExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            && (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detects JPEG / PNG / WEBP from magic bytes. Requires up to 12 bytes for WEBP RIFF header.
    /// </summary>
    public static bool TryDetectImageFormat(ReadOnlySpan<byte> header, out string contentType, out string fileExtension)
    {
        contentType = string.Empty;
        fileExtension = string.Empty;

        if (header.Length >= 3
            && header[0] == 0xFF
            && header[1] == 0xD8
            && header[2] == 0xFF)
        {
            contentType = "image/jpeg";
            fileExtension = ".jpg";
            return true;
        }

        if (header.Length >= 8
            && header[0] == 0x89
            && header[1] == 0x50
            && header[2] == 0x4E
            && header[3] == 0x47
            && header[4] == 0x0D
            && header[5] == 0x0A
            && header[6] == 0x1A
            && header[7] == 0x0A)
        {
            contentType = "image/png";
            fileExtension = ".png";
            return true;
        }

        if (header.Length >= 12
            && header[0] == (byte)'R'
            && header[1] == (byte)'I'
            && header[2] == (byte)'F'
            && header[3] == (byte)'F'
            && header[8] == (byte)'W'
            && header[9] == (byte)'E'
            && header[10] == (byte)'B'
            && header[11] == (byte)'P')
        {
            contentType = "image/webp";
            fileExtension = ".webp";
            return true;
        }

        return false;
    }
}
