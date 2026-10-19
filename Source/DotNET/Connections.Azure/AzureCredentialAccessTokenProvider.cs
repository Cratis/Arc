// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;

namespace Cratis.Arc.Connections.Azure;

/// <summary>
/// Represents an <see cref="IConnectionResolverAccessTokenProvider"/> that gets Microsoft Entra ID tokens through an Azure <see cref="TokenCredential"/>.
/// </summary>
/// <remarks>
/// Provides for <see cref="HttpConnectionResolverAuthenticationType.AzureCredential"/>. The credential follows
/// <see cref="HttpConnectionResolverAuthenticationOptions.Credential"/>: a managed identity (user-assigned when a client id is set),
/// a workload identity, or the default Azure credential chain. The token is requested for
/// <see cref="HttpConnectionResolverAuthenticationOptions.Scope"/>, typically <c>api://&lt;app-id&gt;/.default</c>.
/// </remarks>
public class AzureCredentialAccessTokenProvider : IConnectionResolverAccessTokenProvider
{
    readonly TokenCredential? _credential;
    readonly ConcurrentDictionary<(AzureCredentialType Credential, string? ClientId), TokenCredential> _credentials = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialAccessTokenProvider"/> class that creates the credential from the options.
    /// </summary>
    public AzureCredentialAccessTokenProvider()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialAccessTokenProvider"/> class that always uses the given credential.
    /// </summary>
    /// <param name="credential">The <see cref="TokenCredential"/> to get tokens from.</param>
    public AzureCredentialAccessTokenProvider(TokenCredential credential)
    {
        _credential = credential;
    }

    /// <inheritdoc/>
    public bool CanProvide(HttpConnectionResolverAuthenticationOptions options) =>
        options.Type == HttpConnectionResolverAuthenticationType.AzureCredential;

    /// <inheritdoc/>
    public async ValueTask<ConnectionResolverAccessToken> GetToken(HttpConnectionResolverAuthenticationOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.Scope))
        {
            throw new HttpConnectionResolverAuthenticationNotConfigured(options.Type, nameof(options.Scope));
        }

        var credential = _credential ?? _credentials.GetOrAdd((options.Credential, options.ClientId), key => CreateCredential(key.Credential, key.ClientId));
        var token = await credential.GetTokenAsync(new TokenRequestContext([options.Scope]), cancellationToken);
        return new(token.Token, token.ExpiresOn);
    }

    static TokenCredential CreateCredential(AzureCredentialType type, string? clientId) => type switch
    {
        AzureCredentialType.WorkloadIdentity => new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions { ClientId = clientId }),
        AzureCredentialType.Default => new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = clientId,
            WorkloadIdentityClientId = clientId
        }),
        _ => new ManagedIdentityCredential(string.IsNullOrEmpty(clientId)
            ? ManagedIdentityId.SystemAssigned
            : ManagedIdentityId.FromUserAssignedClientId(clientId))
    };
}
