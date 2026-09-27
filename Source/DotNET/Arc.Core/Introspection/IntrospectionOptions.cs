// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Controls exposure of the command and query catalog endpoints.
/// </summary>
public class IntrospectionOptions
{
    /// <summary>
    /// Gets or sets whether both catalog endpoints are mapped. Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether callers must be authenticated. Defaults to false.
    /// </summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>
    /// Gets or sets comma-separated roles, any one of which grants access. Roles are trimmed; empty roles are rejected. Requires authentication.
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets whether the host trusts identity headers forwarded by a trusted ingress.
    /// Defaults to false; never enable when clients can reach the listener directly or set identity headers.
    /// </summary>
    public bool TrustForwardedIdentityHeaders { get; set; }
}
