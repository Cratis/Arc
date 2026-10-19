// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents how <see cref="HttpConnectionResolver"/> authenticates to its endpoint with a bearer token.
/// </summary>
public class HttpConnectionResolverAuthenticationOptions
{
    /// <summary>
    /// Gets or sets the <see cref="HttpConnectionResolverAuthenticationType"/>. Defaults to <see cref="HttpConnectionResolverAuthenticationType.None"/>.
    /// </summary>
    public HttpConnectionResolverAuthenticationType Type { get; set; } = HttpConnectionResolverAuthenticationType.None;

    /// <summary>
    /// Gets or sets the scope the token is requested for, for instance <c>api://&lt;app-id&gt;/.default</c>.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the OAuth 2.0 token endpoint, used with <see cref="HttpConnectionResolverAuthenticationType.ClientCredentials"/>.
    /// </summary>
    public Uri? TokenEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the client id. With <see cref="HttpConnectionResolverAuthenticationType.ClientCredentials"/> it identifies the client;
    /// with <see cref="HttpConnectionResolverAuthenticationType.AzureCredential"/> it selects a user-assigned managed identity or the workload identity application.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret, used with <see cref="HttpConnectionResolverAuthenticationType.ClientCredentials"/>.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AzureCredentialType"/>, used with <see cref="HttpConnectionResolverAuthenticationType.AzureCredential"/>.
    /// Defaults to <see cref="AzureCredentialType.ManagedIdentity"/>.
    /// </summary>
    public AzureCredentialType Credential { get; set; } = AzureCredentialType.ManagedIdentity;

    /// <summary>
    /// Gets or sets how long before it expires a token is renewed. Defaults to five minutes.
    /// </summary>
    public TimeSpan RefreshMargin { get; set; } = TimeSpan.FromMinutes(5);
}
