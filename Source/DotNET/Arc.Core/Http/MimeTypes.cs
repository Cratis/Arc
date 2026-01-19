// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Provides MIME type mappings for common file extensions.
/// </summary>
public static class MimeTypes
{
    static readonly Dictionary<string, string> _mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text
        [".txt"] = "text/plain",
        [".htm"] = "text/html",
        [".html"] = "text/html",
        [".css"] = "text/css",
        [".csv"] = "text/csv",
        [".xml"] = "text/xml",

        // JavaScript
        [".js"] = "text/javascript",
        [".mjs"] = "text/javascript",
        [".jsx"] = "text/javascript",
        [".ts"] = "text/typescript",
        [".tsx"] = "text/typescript",

        // JSON
        [".json"] = "application/json",
        [".map"] = "application/json",

        // Images
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".ico"] = "image/x-icon",
        [".svg"] = "image/svg+xml",
        [".webp"] = "image/webp",
        [".avif"] = "image/avif",

        // Fonts
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".otf"] = "font/otf",
        [".eot"] = "application/vnd.ms-fontobject",

        // Documents
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".zip"] = "application/zip",
        [".gz"] = "application/gzip",
        [".tar"] = "application/x-tar",
        [".rar"] = "application/vnd.rar",
        [".7z"] = "application/x-7z-compressed",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".m4a"] = "audio/mp4",
        [".flac"] = "audio/flac",
        [".aac"] = "audio/aac",

        // Video
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".mkv"] = "video/x-matroska",

        // Web
        [".wasm"] = "application/wasm",
        [".manifest"] = "text/cache-manifest",
        [".webmanifest"] = "application/manifest+json",

        // Other
        [".bin"] = "application/octet-stream",
        [".dll"] = "application/octet-stream",
        [".exe"] = "application/octet-stream"
    };

    /// <summary>
    /// Gets the MIME type for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns>The MIME type, or "application/octet-stream" if unknown.</returns>
    public static string GetMimeType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return "application/octet-stream";
        }

        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        return _mappings.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }

    /// <summary>
    /// Gets the MIME type for a given file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The MIME type, or "application/octet-stream" if unknown.</returns>
    public static string GetMimeTypeFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return GetMimeType(extension);
    }

    /// <summary>
    /// Adds a custom MIME type mapping.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <param name="mimeType">The MIME type.</param>
    public static void AddMapping(string extension, string mimeType)
    {
        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        _mappings[extension] = mimeType;
    }

    /// <summary>
    /// Adds multiple custom MIME type mappings.
    /// </summary>
    /// <param name="mappings">The mappings to add.</param>
    public static void AddMappings(IEnumerable<KeyValuePair<string, string>> mappings)
    {
        foreach (var mapping in mappings)
        {
            AddMapping(mapping.Key, mapping.Value);
        }
    }
}
