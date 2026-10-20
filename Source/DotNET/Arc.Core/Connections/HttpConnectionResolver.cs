// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an <see cref="IConnectionResolver"/> that resolves by calling an HTTP endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The request is posted as JSON — <c>{ "storage": "SqlServer", "purpose": "Runtime", "scope": { "tenant": "acme" } }</c> —
/// and the endpoint answers with a <see cref="ResolvedConnection"/> as JSON, where every property is optional. An endpoint
/// answering <c>204 No Content</c> leaves the configured connection as it is. Any other unsuccessful status fails.
/// </para>
/// <para>
/// With <see cref="HttpConnectionResolverOptions.Authentication"/> configured, every request carries <c>Authorization: Bearer</c>
/// with a cached token. When the endpoint answers <c>401 Unauthorized</c>, the token is dropped and the request is sent once more
/// with a fresh one.
/// </para>
/// <para>
/// The answer typically carries credentials. It is never logged or persisted, and neither are bearer tokens.
/// </para>
/// </remarks>
/// <param name="options">The <see cref="HttpConnectionResolverOptions"/>.</param>
/// <param name="httpClient">The <see cref="HttpClient"/> to call the endpoint with.</param>
/// <param name="accessTokenProviders">The <see cref="IConnectionResolverAccessTokenProvider"/> instances to choose from when authentication is configured. The first that can provide is used.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> token expiry is checked against.</param>
/// <exception cref="HttpConnectionResolverEndpointNotConfigured">No endpoint is configured.</exception>
/// <exception cref="HttpConnectionResolverAuthorizationHeaderConflict">Both an <c>Authorization</c> header and authentication are configured.</exception>
/// <exception cref="HttpConnectionResolverAuthenticationNotSupported">No provider can provide tokens for the configured authentication.</exception>
[IgnoreConvention]
public class HttpConnectionResolver(
    HttpConnectionResolverOptions options,
    HttpClient httpClient,
    IEnumerable<IConnectionResolverAccessTokenProvider> accessTokenProviders,
    TimeProvider timeProvider) : IConnectionResolver
{
    readonly Uri _endpoint = options.Endpoint ?? throw new HttpConnectionResolverEndpointNotConfigured();
    readonly ConnectionResolverAccessTokens? _accessTokens = CreateAccessTokens(options, accessTokenProviders, timeProvider);

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionResolver"/> class.
    /// </summary>
    /// <param name="options">The <see cref="HttpConnectionResolverOptions"/>.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to call the endpoint with.</param>
    /// <remarks>
    /// Supports <see cref="HttpConnectionResolverAuthenticationType.ClientCredentials"/> authentication, with tokens requested through <paramref name="httpClient"/>.
    /// </remarks>
    public HttpConnectionResolver(HttpConnectionResolverOptions options, HttpClient httpClient)
        : this(options, httpClient, [new ClientCredentialsAccessTokenProvider(httpClient, TimeProvider.System)], TimeProvider.System)
    {
    }

    /// <inheritdoc/>
    public async Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);

        var (response, token) = await Send(request, timeout.Token);
        if (response.StatusCode == HttpStatusCode.Unauthorized && token is not null)
        {
            response.Dispose();
            _accessTokens!.Invalidate(token);
            (response, _) = await Send(request, timeout.Token);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return ResolvedConnection.Unchanged;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ConnectionResolutionFailed(_endpoint, response.StatusCode);
            }

            var resolved = await response.Content.ReadFromJsonAsync(
                ConnectionResolutionJsonSerializerContext.Default.ResolvedConnection,
                timeout.Token);

            return resolved ?? ResolvedConnection.Unchanged;
        }
    }

    static ConnectionResolverAccessTokens? CreateAccessTokens(
        HttpConnectionResolverOptions options,
        IEnumerable<IConnectionResolverAccessTokenProvider> accessTokenProviders,
        TimeProvider timeProvider)
    {
        var authentication = options.Authentication;
        if (authentication.Type == HttpConnectionResolverAuthenticationType.None)
        {
            return null;
        }

        if (options.Headers.Keys.Any(name => string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)))
        {
            throw new HttpConnectionResolverAuthorizationHeaderConflict(authentication.Type);
        }

        var provider = accessTokenProviders.FirstOrDefault(_ => _.CanProvide(authentication))
            ?? throw new HttpConnectionResolverAuthenticationNotSupported(authentication.Type);

        return new(provider, authentication, timeProvider);
    }

    async Task<(HttpResponseMessage Response, string? Token)> Send(ConnectionRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(
                new HttpConnectionRequest(request.Storage, request.Purpose, request.Scope),
                ConnectionResolutionJsonSerializerContext.Default.HttpConnectionRequest)
        };

        foreach (var (name, value) in options.Headers)
        {
            message.Headers.TryAddWithoutValidation(name, value);
        }

        string? token = null;
        if (_accessTokens is not null)
        {
            token = await _accessTokens.Get(cancellationToken);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return (await httpClient.SendAsync(message, cancellationToken), token);
    }
}
