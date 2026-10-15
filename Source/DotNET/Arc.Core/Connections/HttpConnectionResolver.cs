// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;

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
/// The answer typically carries credentials. It is never logged or persisted.
/// </para>
/// </remarks>
/// <param name="options">The <see cref="HttpConnectionResolverOptions"/>.</param>
/// <param name="httpClient">The <see cref="HttpClient"/> to call the endpoint with.</param>
public class HttpConnectionResolver(HttpConnectionResolverOptions options, HttpClient httpClient) : IConnectionResolver
{
    readonly Uri _endpoint = options.Endpoint ?? throw new HttpConnectionResolverEndpointNotConfigured();

    /// <inheritdoc/>
    public async Task<ResolvedConnection> Resolve(ConnectionRequest request, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);

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

        using var response = await httpClient.SendAsync(message, timeout.Token);
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
