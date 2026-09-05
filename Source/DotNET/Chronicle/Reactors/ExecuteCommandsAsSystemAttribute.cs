// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Marks a reactor so that commands it returns as side effects execute as a trusted system actor carrying the given roles.
/// </summary>
/// <remarks>
/// Commands returned from a reactor run server-side with no HTTP request, so any command carrying
/// <see cref="Authorization.AuthorizeAttribute"/> or <see cref="Authorization.RolesAttribute"/> would otherwise be
/// denied. Applying this attribute makes those returned commands execute as an authenticated system actor holding
/// the declared roles.
/// </remarks>
/// <param name="roles">The roles the reactor's returned commands execute under.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExecuteCommandsAsSystemAttribute(params string[] roles) : Attribute
{
    /// <summary>
    /// Gets the roles the reactor's returned commands execute under.
    /// </summary>
    public IReadOnlyList<string> Roles { get; } = roles;
}
