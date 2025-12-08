// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Options for creating HTTP cookies.
/// </summary>
public class CookieOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the cookie is HTTP only.
    /// </summary>
    public bool HttpOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cookie requires HTTPS.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Gets or sets the SameSite mode for the cookie.
    /// </summary>
    public SameSiteMode SameSite { get; set; }

    /// <summary>
    /// Gets or sets the path for the cookie.
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Gets or sets the expiration time for the cookie.
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    /// <summary>
    /// Gets or sets the max age for the cookie.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the domain for the cookie.
    /// </summary>
    public string? Domain { get; set; }
}
