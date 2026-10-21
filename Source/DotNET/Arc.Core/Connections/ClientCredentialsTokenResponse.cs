// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the parts of an OAuth 2.0 token response <see cref="ClientCredentialsAccessTokenProvider"/> reads.
/// </summary>
/// <param name="AccessToken">The issued access token.</param>
/// <param name="ExpiresIn">The lifetime of the token in seconds, if given.</param>
public record ClientCredentialsTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("expires_in"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] long? ExpiresIn)
{
    /// <inheritdoc/>
    public override string ToString() => $"Token response expiring in {ExpiresIn?.ToString() ?? "unknown"} seconds";
}
