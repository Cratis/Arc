// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents what an artifact requires of the caller.
/// </summary>
/// <param name="RequiresAuthentication">Whether the caller has to be authenticated.</param>
/// <param name="Roles">The roles the caller may hold - any one of them is sufficient.</param>
public record AuthorizationModel(bool RequiresAuthentication, IEnumerable<string> Roles)
{
    /// <summary>
    /// Represents an artifact that is open to anonymous callers.
    /// </summary>
    public static readonly AuthorizationModel None = new(false, []);

    /// <summary>
    /// Gets the named policies the caller has to satisfy, all of them.
    /// </summary>
    /// <remarks>
    /// A policy is named on the artifact but declared where the application is composed, so the name is all the
    /// artifact itself says. Carrying it is what keeps a document from flattening every policy into the one thing
    /// they all have in common, which is that somebody has to be there at all.
    /// </remarks>
    public IEnumerable<string> Policies { get; init; } = [];
}
