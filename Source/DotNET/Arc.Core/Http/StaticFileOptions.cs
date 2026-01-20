// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Options for static file serving middleware.
/// </summary>
public class StaticFileOptions
{
    /// <summary>
    /// Gets or sets the file system path to serve files from.
    /// </summary>
    public string FileSystemPath { get; set; } = "wwwroot";

    /// <summary>
    /// Gets or sets the request path prefix. Defaults to empty string (root).
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to serve default files (e.g., index.html).
    /// </summary>
    public bool ServeDefaultFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets the default file names to look for.
    /// </summary>
    public IList<string> DefaultFileNames { get; set; } = ["index.html", "index.htm", "default.html", "default.htm"];

    /// <summary>
    /// Gets or sets a value indicating whether directory browsing is enabled.
    /// </summary>
    public bool EnableDirectoryBrowsing { get; set; }

    /// <summary>
    /// Gets or sets custom content type mappings by file extension.
    /// </summary>
    public IDictionary<string, string> ContentTypeMappings { get; set; } = new Dictionary<string, string>();
}
