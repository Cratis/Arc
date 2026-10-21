// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents the hooks that let a MongoDB client follow a resolved connection after it has been created.
/// </summary>
/// <remarks>
/// <para>
/// As an <see cref="IOidcCallback"/> it hands the driver the resolved access token. The driver only asks when it has no
/// valid token — at first, when one expires, or when the server rejected one — so the cached resolution is dropped first.
/// </para>
/// <para>
/// Through <see cref="ConfigureCluster"/> it drops the cached resolution when a connection fails to authenticate, so the
/// next connection resolved for the request picks up rotated credentials.
/// </para>
/// <para>
/// Keep one instance per request: the driver compares credentials, and so callbacks, when deciding whether two
/// settings describe the same client.
/// </para>
/// </remarks>
/// <param name="resolver">The <see cref="ICachingConnectionResolver"/> to resolve through.</param>
/// <param name="request">The <see cref="ConnectionRequest"/> to resolve.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> for the current time.</param>
[IgnoreConvention]
public class MongoDBConnectionResolutionHooks(ICachingConnectionResolver resolver, ConnectionRequest request, TimeProvider timeProvider) : IOidcCallback
{
    /// <inheritdoc/>
    public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken) =>
        GetOidcAccessTokenAsync(parameters, cancellationToken).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
    {
        resolver.Invalidate(request);
        var resolved = await resolver.Resolve(request, cancellationToken);
        var accessToken = resolved.Identity?.AccessToken ?? throw new MongoDBAccessTokenNotResolved(request);
        return new(accessToken, resolved.ExpiresAt - timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Configure the cluster to drop the cached resolution when a connection fails to authenticate.
    /// </summary>
    /// <param name="builder">The <see cref="ClusterBuilder"/>.</param>
    public void ConfigureCluster(ClusterBuilder builder) =>
        builder.Subscribe<ConnectionOpeningFailedEvent>(OnConnectionOpeningFailed);

    /// <summary>
    /// Handle a connection that failed to open.
    /// </summary>
    /// <param name="event">The <see cref="ConnectionOpeningFailedEvent"/>.</param>
    internal void OnConnectionOpeningFailed(ConnectionOpeningFailedEvent @event)
    {
        if (IsAuthenticationFailure(@event.Exception))
        {
            resolver.Invalidate(request);
        }
    }

    static bool IsAuthenticationFailure(Exception? exception) => exception switch
    {
        null => false,
        MongoAuthenticationException => true,
        _ => IsAuthenticationFailure(exception.InnerException)
    };
}
