// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="IConnectionResolverAccessTokenProvider"/> for the OAuth 2.0 client credentials grant.
/// </summary>
/// <remarks>
/// Posts <c>grant_type=client_credentials</c> with the client id, the client secret and the scope to the configured token endpoint,
/// and reads <c>access_token</c> and <c>expires_in</c> from the answer. A token without <c>expires_in</c> is used for one call only.
/// The secret and the token are never logged.
/// </remarks>
/// <param name="httpClient">The <see cref="HttpClient"/> to call the token endpoint with.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> expiry is computed from.</param>
public class ClientCredentialsAccessTokenProvider(HttpClient httpClient, TimeProvider timeProvider) : IConnectionResolverAccessTokenProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsAccessTokenProvider"/> class with the shared connection resolution <see cref="HttpClient"/>.
    /// </summary>
    public ClientCredentialsAccessTokenProvider()
        : this(ConnectionResolutionHttpClient.Instance, TimeProvider.System)
    {
    }

    /// <inheritdoc/>
    public bool CanProvide(HttpConnectionResolverAuthenticationOptions options) =>
        options.Type == HttpConnectionResolverAuthenticationType.ClientCredentials;

    /// <inheritdoc/>
    /// <exception cref="ConnectionResolverTokenEndpointMustUseHttps">The token endpoint does not use HTTPS or HTTP on a loopback host.</exception>
    public async ValueTask<ConnectionResolverAccessToken> GetToken(HttpConnectionResolverAuthenticationOptions options, CancellationToken cancellationToken)
    {
        var tokenEndpoint = options.TokenEndpoint ?? throw NotConfigured(nameof(options.TokenEndpoint));
        if (!tokenEndpoint.IsAbsoluteUri ||
            (tokenEndpoint.Scheme != Uri.UriSchemeHttps && !(tokenEndpoint.Scheme == Uri.UriSchemeHttp && tokenEndpoint.IsLoopback)))
        {
            throw new ConnectionResolverTokenEndpointMustUseHttps();
        }

        var clientId = string.IsNullOrEmpty(options.ClientId) ? throw NotConfigured(nameof(options.ClientId)) : options.ClientId;
        var clientSecret = string.IsNullOrEmpty(options.ClientSecret) ? throw NotConfigured(nameof(options.ClientSecret)) : options.ClientSecret;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };
        if (!string.IsNullOrEmpty(options.Scope))
        {
            form["scope"] = options.Scope;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) { Content = new FormUrlEncodedContent(form) };
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ConnectionResolverAccessTokenRequestFailed(tokenEndpoint, clientId, options.Scope, response.StatusCode);
        }

        var issued = await response.Content.ReadFromJsonAsync(
            ConnectionResolutionJsonSerializerContext.Default.ClientCredentialsTokenResponse,
            cancellationToken);
        if (string.IsNullOrEmpty(issued?.AccessToken))
        {
            throw new ConnectionResolverAccessTokenRequestFailed(tokenEndpoint, clientId, options.Scope, response.StatusCode);
        }

        var now = timeProvider.GetUtcNow();
        return new(issued.AccessToken, issued.ExpiresIn is { } seconds ? now.AddSeconds(seconds) : now);
    }

    static HttpConnectionResolverAuthenticationNotConfigured NotConfigured(string setting) =>
        new(HttpConnectionResolverAuthenticationType.ClientCredentials, setting);
}
