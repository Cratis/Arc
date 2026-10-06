// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// ASP.NET Core implementation of <see cref="IEndpointMapper"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AspNetCoreEndpointMapper"/> class.
/// </remarks>
/// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
/// <param name="groupPrefix">Optional group prefix for all routes.</param>
public class AspNetCoreEndpointMapper(IEndpointRouteBuilder endpoints, string? groupPrefix = null) : IEndpointMapper, IIntrospectionExposureGuard
{
    readonly RouteGroupBuilder _group = string.IsNullOrEmpty(groupPrefix)
            ? endpoints.MapGroup(string.Empty)
            : endpoints.MapGroup(groupPrefix);

    readonly HashSet<string> _mapped = new(StringComparer.Ordinal);
    IReadOnlySet<string>? _preExisting;

    /// <inheritdoc/>
    IServiceProvider? IIntrospectionExposureGuard.Services => endpoints.ServiceProvider;

    /// <summary>
    /// Gets the names of the endpoints that were already registered when this mapper started mapping.
    /// </summary>
    /// <remarks>
    /// Taken once, on first use, rather than per registration. Asking the route builder is not a lookup - it
    /// rebuilds the entire endpoint table (see <c>EndpointNames</c>) - so doing it for every endpoint made
    /// mapping cost grow with the square of the number of commands and queries.
    /// A mapper is created immediately before the pass that uses it and nothing else registers endpoints during
    /// that pass, so a single snapshot plus the names this mapper has since added is the same answer.
    /// </remarks>
    IReadOnlySet<string> PreExisting => _preExisting ??= endpoints.EndpointNames();

    /// <inheritdoc/>
    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        Map("GET", pattern, handler, metadata);

    /// <inheritdoc/>
    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        Map("POST", pattern, handler, metadata);

    /// <inheritdoc/>
    public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        Map(httpMethod, pattern, handler, metadata);

    /// <inheritdoc/>
    public bool EndpointExists(string name) => _mapped.Contains(name) || PreExisting.Contains(name);

    /// <inheritdoc/>
    string? IIntrospectionExposureGuard.FindEnforcementProblem(IServiceProvider? services)
    {
        services ??= endpoints.ServiceProvider;
        using var scope = services.CreateScope();
        if (scope.ServiceProvider.GetService<IAuthenticationSchemeProvider>()?.GetDefaultAuthenticateSchemeAsync().GetAwaiter().GetResult() is null)
        {
            return "Requiring authentication on the discovery endpoints needs a default ASP.NET Core authentication scheme.";
        }

        var hasAuthorization = services.GetService<IServiceProviderIsService>()?.IsService(typeof(IAuthorizationService))
            ?? (scope.ServiceProvider.GetService<IAuthorizationService>() is not null);

        return !hasAuthorization
            ? "Requiring authentication on the discovery endpoints needs ASP.NET Core authorization services (AddAuthorization)."
            : null;
    }

    void Map(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata)
    {
        Delegate requestHandler = async (HttpContext httpContext) =>
        {
            var context = new AspNetCoreHttpRequestContext(httpContext);
            var accessor = httpContext.RequestServices.GetRequiredService<IHttpRequestContextAccessor>();
            accessor.Current = context;
            await handler(context);
        };

        var builder = _group.MapMethods(pattern, [httpMethod], requestHandler);

        ApplyMetadata(builder, metadata);
    }

    void ApplyMetadata(RouteHandlerBuilder builder, EndpointMetadata? metadata)
    {
        if (metadata is null) return;

        if (metadata.ExcludeFromApiDescription)
        {
            builder.ExcludeFromDescription();
        }

        builder.WithName(metadata.Name);
        _mapped.Add(metadata.Name);

        if (!string.IsNullOrEmpty(metadata.Summary))
        {
            builder.WithSummary(metadata.Summary);
        }

        if (metadata.Tags?.Any() == true)
        {
            builder.WithTags(metadata.Tags.ToArray());
        }

        if (metadata.AllowAnonymous)
        {
            builder.AllowAnonymous();
        }
        else if (metadata.RequireAuthentication)
        {
            using var scope = endpoints.ServiceProvider.CreateScope();
            var defaultPolicy = scope.ServiceProvider.GetService<IAuthorizationPolicyProvider>()?.GetDefaultPolicyAsync().GetAwaiter().GetResult();
            var policy = defaultPolicy is null ? new AuthorizationPolicyBuilder() : new AuthorizationPolicyBuilder(defaultPolicy);
            policy.RequireAuthenticatedUser();
            if (metadata.Roles is not null)
            {
                policy.RequireRole(metadata.Roles.Split(',').Select(role => role.Trim()).ToArray());
            }
            builder.RequireAuthorization(policy.Build());
        }

        if (metadata.RequestBodyType is not null)
        {
            builder.Accepts(metadata.RequestBodyType, "application/json");
        }

        if (metadata.ResponseType is not null)
        {
            builder.Produces(200, metadata.ResponseType, "application/json");
        }
    }
}
