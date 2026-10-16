// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the result of resolving a connection.
/// </summary>
/// <remarks>
/// Every part is optional. A part that is not set leaves the configured value as it is, so a resolver can answer only
/// for the parts it owns — for instance only the <see cref="Identity"/>.
/// </remarks>
/// <param name="ConnectionString">Optional connection string that replaces the configured one.</param>
/// <param name="Database">Optional name of the database to use.</param>
/// <param name="Schema">Optional name of the schema to use, for storage that has schemas.</param>
/// <param name="Identity">Optional <see cref="ConnectionIdentity"/> to authenticate with.</param>
/// <param name="ExpiresAt">Optional point in time after which the result must be resolved again.</param>
public record ResolvedConnection(
    string? ConnectionString = null,
    string? Database = null,
    string? Schema = null,
    ConnectionIdentity? Identity = null,
    DateTimeOffset? ExpiresAt = null)
{
    /// <summary>
    /// Gets a <see cref="ResolvedConnection"/> that leaves everything as configured.
    /// </summary>
    public static readonly ResolvedConnection Unchanged = new();

    /// <summary>
    /// Merge another <see cref="ResolvedConnection"/> on top of this one.
    /// </summary>
    /// <param name="other">The <see cref="ResolvedConnection"/> whose parts win when set.</param>
    /// <returns>The merged <see cref="ResolvedConnection"/>.</returns>
    /// <remarks>
    /// The <see cref="Identity"/> is replaced as a whole rather than merged, so a password never ends up next to an
    /// access token from another resolver. The earliest <see cref="ExpiresAt"/> wins.
    /// </remarks>
    public ResolvedConnection Merge(ResolvedConnection other) => new(
        other.ConnectionString ?? ConnectionString,
        other.Database ?? Database,
        other.Schema ?? Schema,
        other.Identity ?? Identity,
        Earliest(ExpiresAt, other.ExpiresAt));

    static DateTimeOffset? Earliest(DateTimeOffset? left, DateTimeOffset? right) => (left, right) switch
    {
        (null, _) => right,
        (_, null) => left,
        _ => left < right ? left : right
    };
}
