// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.CrossCuttingAuthorization;

/// <summary>
/// Represents a read model for showcasing cross-cutting authorization with query filters.
/// </summary>
/// <param name="Message">A human-readable status message.</param>
/// <param name="CheckedAt">The timestamp for when the status was generated.</param>
[ReadModel]
public record CrossCuttingAuthorizationStatus(string Message, DateTimeOffset CheckedAt)
{
    /// <summary>
    /// Gets the current secured status.
    /// </summary>
    /// <returns>A <see cref="CrossCuttingAuthorizationStatus"/> describing the secured query execution.</returns>
    public static CrossCuttingAuthorizationStatus Secured() =>
        new("Query authorized and executed through a custom query filter.", DateTimeOffset.UtcNow);
}
