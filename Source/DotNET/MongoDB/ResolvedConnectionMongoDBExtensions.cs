// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.Oidc;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Provides extension methods for applying a <see cref="ResolvedConnection"/> to MongoDB.
/// </summary>
public static class ResolvedConnectionMongoDBExtensions
{
    /// <summary>
    /// The authentication database used when neither the connection string nor its database name says otherwise.
    /// </summary>
    public const string DefaultAuthenticationSource = "admin";

    /// <summary>
    /// Create <see cref="MongoClientSettings"/> from a <see cref="ResolvedConnection"/> applied over a configured server.
    /// </summary>
    /// <param name="resolved">The <see cref="ResolvedConnection"/>.</param>
    /// <param name="configured">The configured <see cref="MongoUrl"/>, used when no connection string was resolved.</param>
    /// <param name="accessTokenCallback">The <see cref="IOidcCallback"/> giving access tokens; required for refreshing tokens.
    /// When not given, a resolved access token is used as it is.</param>
    /// <returns>The <see cref="MongoClientSettings"/>.</returns>
    /// <remarks>
    /// A user name and password become a SCRAM credential against the authentication source of the connection string, else
    /// its database, else <see cref="DefaultAuthenticationSource"/>. An access token becomes a MONGODB-OIDC credential.
    /// </remarks>
    public static MongoClientSettings ToMongoClientSettings(this ResolvedConnection resolved, MongoUrl configured, IOidcCallback? accessTokenCallback = null)
    {
        var url = string.IsNullOrEmpty(resolved.ConnectionString) ? configured : new MongoUrl(resolved.ConnectionString);
        var settings = MongoClientSettings.FromUrl(url);

        settings.Credential = resolved.Identity switch
        {
            { AccessToken: { } accessToken } identity => MongoCredential.CreateOidcCredential(accessTokenCallback ?? new FixedAccessToken(accessToken), identity.UserName),
            { UserName: { } userName, Password: { } password } => MongoCredential.CreateCredential(url.AuthenticationSource ?? url.DatabaseName ?? DefaultAuthenticationSource, userName, password),
            _ => settings.Credential
        };

        return settings;
    }

    sealed class FixedAccessToken(string accessToken) : IOidcCallback
    {
        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken) => new(accessToken, null);

        public Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken) =>
            Task.FromResult(new OidcAccessToken(accessToken, null));
    }
}
