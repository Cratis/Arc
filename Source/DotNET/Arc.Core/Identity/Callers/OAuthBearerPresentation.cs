// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc.Identity.Callers;

/// <summary>An exact OAuth token and immutable expectations from the verified signed proof.</summary>
/// <remarks>These expectations are not token validation. Do not log <see cref="AccessToken"/>.</remarks>
public sealed class OAuthBearerPresentation
{
    /// <summary>Initializes a new instance of the <see cref="OAuthBearerPresentation"/> class.</summary>
    /// <param name="token">The exact covered token.</param>
    /// <param name="context">The verified signed context.</param>
    /// <param name="target">The receiving installation.</param>
    /// <param name="scopes">The optional signed scope ceiling.</param>
    internal OAuthBearerPresentation(string token, ForwardedIdentityV2Context context, CallerCredentialTarget target, IReadOnlySet<string>? scopes)
    {
        AccessToken = token;
        Issuer = context.Issuer!;
        Subject = context.Subject!;
        CredentialId = context.CredentialId;
        Resource = target.Resource;
        Audience = target.Audience;
        Environment = target.Environment;
        Tenant = context.TenantTargets.Count > 0 ? context.TenantTargets[0] : null;
        Scopes = scopes;
        ClientId = context.TokenClientId;
    }

    /// <summary>Gets the exact token, without the authorization scheme. Never log it.</summary>
    public string AccessToken { get; }

    /// <summary>Gets the signed account issuer.</summary>
    public string Issuer { get; }

    /// <summary>Gets the signed account subject.</summary>
    public string Subject { get; }

    /// <summary>Gets the signed credential identifier.</summary>
    public string CredentialId { get; }

    /// <summary>Gets the receiving installation's resource.</summary>
    public string Resource { get; }

    /// <summary>Gets the receiving installation's audience.</summary>
    public string Audience { get; }

    /// <summary>Gets the receiving installation's environment.</summary>
    public string Environment { get; }

    /// <summary>Gets the signed tenant, or null for a tenantless proof.</summary>
    public string? Tenant { get; }

    /// <summary>Gets the signed scope ceiling, or null when absent.</summary>
    public IReadOnlySet<string>? Scopes { get; }

    /// <summary>Gets the signed client identifier, or null when absent.</summary>
    public string? ClientId { get; }

    /// <inheritdoc/>
    public override string ToString() => "OAuth bearer presentation [redacted]";
}
