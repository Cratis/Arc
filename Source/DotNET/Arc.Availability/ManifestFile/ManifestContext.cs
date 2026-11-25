// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// The context a part is decided in when it is decided from a manifest file rather than from a running application.
/// </summary>
/// <param name="Tenant">The tenant; empty when there is none.</param>
/// <param name="User">The user; empty when there is none.</param>
/// <param name="Audiences">The audiences held.</param>
/// <param name="Suspended">The paths that are suspended.</param>
/// <param name="Values">Further context values by name, for a gate that reads one.</param>
/// <param name="Arguments">The arguments of the command or query the question is about; none when it is about the part alone.</param>
public record ManifestContext(
    string Tenant,
    string User,
    IReadOnlyList<string> Audiences,
    IReadOnlyList<string> Suspended,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyDictionary<string, object?>? Arguments)
{
    /// <summary>
    /// Gets a context with no tenant, user, audiences, suspensions, values or arguments.
    /// </summary>
    public static ManifestContext Empty { get; } = new(string.Empty, string.Empty, [], [], new Dictionary<string, object?>(), null);
}
