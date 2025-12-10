// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Specifies the SameSite mode for a cookie.
/// </summary>
public enum SameSiteMode
{
    /// <summary>
    /// No SameSite attribute.
    /// </summary>
    Unspecified = -1,

    /// <summary>
    /// No restrictions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Lax mode - cookies are sent with top-level navigations.
    /// </summary>
    Lax = 1,

    /// <summary>
    /// Strict mode - cookies are not sent on any cross-site requests.
    /// </summary>
    Strict = 2
}
