// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Controls access to the query health diagnostic feed on direct endpoints and hub subscriptions.
/// </summary>
public class QueryHealthOptions
{
    /// <summary>
    /// Gets or sets whether callers of the query health feed must be authenticated.
    /// </summary>
    /// <remarks>
    /// When unset, the feed allows anonymous callers in Development and requires authentication elsewhere.
    /// The getter returns false when unset; only an explicit assignment overrides the environment default.
    /// Set true to require authentication in every environment, or false to allow anonymous access when no roles are configured.
    /// </remarks>
    public bool RequireAuthentication
    {
        get => AuthenticationOverride ?? false;
        set => AuthenticationOverride = value;
    }

    /// <summary>
    /// Gets or sets comma-separated roles, any one of which grants access. Roles are trimmed and empty entries ignored.
    /// Configuring roles requires authentication in every environment, even if <see cref="RequireAuthentication"/> is false.
    /// </summary>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets the explicit authentication override for configuration binding.
    /// </summary>
    internal bool? AuthenticationOverride { get; set; }

    /// <summary>
    /// Resolves authentication using the explicit settings and host environment.
    /// </summary>
    /// <param name="isDevelopment">Whether the host is in Development.</param>
    /// <returns>Whether the feed requires an authenticated caller.</returns>
    internal bool RequiresAuthentication(bool isDevelopment) => Roles is not null || (AuthenticationOverride ?? !isDevelopment);
}
