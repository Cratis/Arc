// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents a bearer token <see cref="HttpConnectionResolver"/> authenticates to its endpoint with.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="ExpiresAt">When the token expires.</param>
public record ConnectionResolverAccessToken(string Token, DateTimeOffset ExpiresAt)
{
    /// <inheritdoc/>
    public override string ToString() => $"Access token expiring at {ExpiresAt:O}";
}
