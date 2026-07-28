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
}
