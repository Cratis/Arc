// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Controls exposure of the discovery endpoints: the command and query catalogs (<c>/.cratis/commands</c> and
/// <c>/.cratis/queries</c>) and identity discovery (<c>/.cratis/users</c>, <c>/.cratis/tenants</c> and
/// <c>/.cratis/identity-details/schema</c>).
/// </summary>
public class IntrospectionOptions
{
    /// <summary>
    /// Gets or sets whether both catalog endpoints are mapped. Defaults to true. Does not control identity discovery.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether users, tenants and the identity details schema discovery endpoints are mapped. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Set this and <see cref="Enabled"/> to false to remove every discovery endpoint without evaluating authentication
    /// enforcement. This does not affect <c>/.cratis/me</c>. Access settings apply only to mapped discovery endpoints;
    /// role configuration is still validated when discovery is disabled.
    /// </remarks>
    public bool IdentityDiscovery { get; set; } = true;

    /// <summary>
    /// Gets or sets whether callers of the discovery endpoints must be authenticated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When not set, the default, the endpoints are anonymous in Development and require an authenticated caller
    /// everywhere else. The getter returns false when unset, but only an explicit assignment overrides the environment default.
    /// Set it to <see langword="false"/> to expose them anonymously in every environment, or to
    /// <see langword="true"/> to require authentication in every environment, including Development.
    /// </para>
    /// <para>
    /// When it is not set and the host has no way to authenticate callers, such as an ASP.NET Core host without a
    /// default authentication scheme, the discovery endpoints are not mapped outside Development. When it is
    /// <see langword="true"/>, the same host fails at startup instead.
    /// </para>
    /// </remarks>
    public bool RequireAuthentication
    {
        get => AuthenticationOverride ?? false;
        set => AuthenticationOverride = value;
    }

    /// <summary>
    /// Gets or sets comma-separated roles, any one of which grants access. Roles are trimmed; empty roles are rejected.
    /// Setting roles requires an authenticated caller in every environment, and cannot be combined with
    /// <see cref="RequireAuthentication"/> set to <see langword="false"/>.
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets whether the host trusts identity headers forwarded by a trusted ingress.
    /// </summary>
    /// <remarks>
    /// Trust in forwarded identity headers is now a host-wide setting that applies to every request, not only to the
    /// catalogs. Setting this to <see langword="true"/> still turns on <see cref="ArcOptions.TrustForwardedIdentityHeaders"/>.
    /// </remarks>
    [Obsolete("Use ArcOptions.TrustForwardedIdentityHeaders (Cratis:Arc:TrustForwardedIdentityHeaders), which applies to every request. This setting turns it on and will be removed in a future major version.")]
    public bool TrustForwardedIdentityHeaders { get; set; }

    /// <summary>
    /// Gets or sets the explicit override for configuration binding.
    /// </summary>
    internal bool? AuthenticationOverride { get; set; }

    /// <summary>
    /// Gets whether authentication was asked for explicitly, rather than by the environment default.
    /// </summary>
    internal bool AuthenticationExplicitlyRequired => AuthenticationOverride == true || Roles is not null;

    /// <summary>
    /// Gets whether anonymous exposure was requested explicitly.
    /// </summary>
    internal bool AuthenticationExplicitlyDisabled => AuthenticationOverride == false;

    /// <summary>
    /// Decides whether callers of the discovery endpoints must be authenticated.
    /// </summary>
    /// <param name="isDevelopment">Whether the host runs in the Development environment.</param>
    /// <returns>True if callers must be authenticated.</returns>
    internal bool RequiresAuthentication(bool isDevelopment) => AuthenticationExplicitlyRequired || (AuthenticationOverride ?? !isDevelopment);
}
