// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the identity a connection authenticates with.
/// </summary>
/// <remarks>
/// Set either a user name and password, or an access token. An access token is used as an Entra ID token for SQL
/// Server, as the password for PostgreSQL and as an OIDC token for MongoDB.
/// </remarks>
/// <param name="UserName">Optional user name.</param>
/// <param name="Password">Optional password.</param>
/// <param name="AccessToken">Optional access token.</param>
public record ConnectionIdentity(string? UserName = null, string? Password = null, string? AccessToken = null)
{
    /// <inheritdoc/>
    /// <remarks>
    /// Never includes the password or the access token, so an identity can be logged.
    /// </remarks>
    public override string ToString() => $"{nameof(ConnectionIdentity)} {{ UserName = {UserName}, HasPassword = {Password is not null}, HasAccessToken = {AccessToken is not null} }}";
}
