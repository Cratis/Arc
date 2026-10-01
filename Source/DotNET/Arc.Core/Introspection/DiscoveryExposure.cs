// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Decides how the discovery endpoints - the command and query catalogs and identity discovery - are exposed.
/// </summary>
internal static class DiscoveryExposure
{
    static int _reportedAnonymous;
    static int _reportedUnavailable;

    /// <summary>
    /// Decides how the discovery endpoints are exposed by the given mapper.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> that maps the endpoints.</param>
    /// <param name="options">The discovery exposure settings.</param>
    /// <param name="logger">Optional logger to report exposure that needs attention.</param>
    /// <returns>The <see cref="DiscoveryAccess"/> to map the endpoints with.</returns>
    /// <exception cref="InvalidIntrospectionConfiguration">The settings are invalid, or authentication is explicitly required and the host cannot enforce it.</exception>
    internal static DiscoveryAccess Resolve(IEndpointMapper mapper, IntrospectionOptions options, ILogger? logger = null) =>
        Resolve(mapper, options, RuntimeEnvironment.IsDevelopment, logger);

    /// <summary>
    /// Decides how the discovery endpoints are exposed by the given mapper in the given environment.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> that maps the endpoints.</param>
    /// <param name="options">The discovery exposure settings.</param>
    /// <param name="isDevelopment">Whether the host runs in the Development environment.</param>
    /// <param name="logger">Optional logger to report exposure that needs attention.</param>
    /// <returns>The <see cref="DiscoveryAccess"/> to map the endpoints with.</returns>
    /// <exception cref="InvalidIntrospectionConfiguration">The settings are invalid, or authentication is explicitly required and the host cannot enforce it.</exception>
    internal static DiscoveryAccess Resolve(IEndpointMapper mapper, IntrospectionOptions options, bool isDevelopment, ILogger? logger = null)
    {
        ThrowIfInvalid(options);

        if (!options.RequiresAuthentication(isDevelopment))
        {
            if (!isDevelopment && logger is not null && Interlocked.Exchange(ref _reportedAnonymous, 1) == 0)
            {
                logger.DiscoveryExposedAnonymously();
            }

            return DiscoveryAccess.Anonymous;
        }

        if (mapper is IIntrospectionExposureGuard guard && guard.FindEnforcementProblem() is string problem)
        {
            if (options.AuthenticationExplicitlyRequired)
            {
                throw new InvalidIntrospectionConfiguration(problem);
            }

            if (logger is not null && Interlocked.Exchange(ref _reportedUnavailable, 1) == 0)
            {
                logger.DiscoveryNotMapped(problem);
            }

            return DiscoveryAccess.Unavailable;
        }

        return DiscoveryAccess.Authenticated;
    }

    /// <summary>
    /// Throws if the discovery exposure settings are invalid.
    /// </summary>
    /// <param name="options">The discovery exposure settings.</param>
    /// <exception cref="InvalidIntrospectionConfiguration">The settings are invalid.</exception>
    internal static void ThrowIfInvalid(IntrospectionOptions options)
    {
        var validation = IntrospectionOptionsValidator.ValidateOptions(options);
        if (validation.Failed)
        {
            throw new InvalidIntrospectionConfiguration(string.Join(' ', validation.Failures));
        }
    }

    /// <summary>
    /// Creates the endpoint metadata for a discovery endpoint.
    /// </summary>
    /// <param name="access">The <see cref="DiscoveryAccess"/> to map the endpoint with.</param>
    /// <param name="options">The discovery exposure settings.</param>
    /// <param name="name">The endpoint name.</param>
    /// <param name="summary">The endpoint summary.</param>
    /// <param name="tag">The endpoint tag.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The <see cref="EndpointMetadata"/>.</returns>
    internal static EndpointMetadata MetadataFor(DiscoveryAccess access, IntrospectionOptions options, string name, string summary, string tag, Type responseType)
    {
        var authenticated = access == DiscoveryAccess.Authenticated;
        return new EndpointMetadata(name, summary, [tag], AllowAnonymous: !authenticated, ResponseType: responseType)
        {
            RequireAuthentication = authenticated,
            Roles = authenticated ? options.Roles : null
        };
    }
}
