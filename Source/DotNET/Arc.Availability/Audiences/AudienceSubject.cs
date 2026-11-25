// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Identifies who a request is for, so the audiences that apply to it can be resolved.
/// </summary>
/// <param name="Tenant">The tenant, or <see langword="null"/> when the request names none.</param>
/// <param name="User">The user, or <see langword="null"/> when the request has no user.</param>
/// <param name="IsSystem">Whether the caller is the system itself, which holds system-level audiences.</param>
public record AudienceSubject(string? Tenant, string? User, bool IsSystem = false)
{
    readonly string _systemClientId = string.Empty;

    /// <summary>
    /// Gets the client id of the calling system, or an empty string when the caller names none.
    /// </summary>
    /// <remarks>
    /// An <see cref="IAudienceSource"/> uses it to return the system-wide grants plus the calling system's own. A caller
    /// that names no client id gets the system-wide grants only. Set it with an object initializer or a <see langword="with"/> expression.
    /// </remarks>
    public string SystemClientId
    {
        get => _systemClientId;
        init => _systemClientId = value ?? string.Empty;
    }
}
