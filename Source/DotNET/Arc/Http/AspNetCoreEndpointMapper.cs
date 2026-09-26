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
    bool _trustForwardedIdentityHeaders;

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
    void IIntrospectionExposureGuard.Validate(IntrospectionOptions options)
    {
        var services = endpoints.ServiceProvider;
        var schemes = services.GetService<IAuthenticationSchemeProvider>();
        var defaultScheme = schemes?.GetDefaultAuthenticateSchemeAsync().GetAwaiter().GetResult() ??
            throw new InvalidIntrospectionConfiguration("Introspection requires a default ASP.NET Core authentication scheme when RequireAuthentication is true.");
        if (services.GetService<IAuthorizationService>() is null)
        {
            throw new InvalidIntrospectionConfiguration("Introspection requires ASP.NET Core authorization services (AddAuthorization) when RequireAuthentication is true.");
        }
        if (!options.TrustForwardedIdentityHeaders && UnsignedIdentityHeaderSchemes.IsReachable(services, schemes, defaultScheme))
        {
            throw new InvalidIntrospectionConfiguration("The default ASP.NET Core authentication scheme trusts unsigned x-ms-client-principal headers. Protected introspection requires a trusted ingress (such as Azure App Service or Container Apps EasyAuth) that strips and sets these headers. Set Cratis:Arc:Introspection:TrustForwardedIdentityHeaders=true to opt in, or use a different authentication scheme.");
        }
        _trustForwardedIdentityHeaders = options.TrustForwardedIdentityHeaders;
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
            var defaultPolicy = endpoints.ServiceProvider.GetService<IAuthorizationPolicyProvider>()?.GetDefaultPolicyAsync().GetAwaiter().GetResult();
            var policy = defaultPolicy is null ? new AuthorizationPolicyBuilder() : new AuthorizationPolicyBuilder(defaultPolicy);
            policy.RequireAuthenticatedUser();
            if (metadata.Roles is not null)
            {
                policy.RequireRole(metadata.Roles.Split(',').Select(role => role.Trim()).ToArray());
            }
            builder.RequireAuthorization(policy.Build());

            if ((metadata.Name == IntrospectionEndpointMapper.CommandsEndpointName ||
                 metadata.Name == IntrospectionEndpointMapper.QueriesEndpointName) &&
                !_trustForwardedIdentityHeaders)
            {
                builder.AddEndpointFilter(async (context, next) =>
                {
                    if (await UnsignedIdentityHeaderSchemes.AuthenticatedWithHeaders(context.HttpContext))
                    {
                        return Results.Challenge();
                    }

                    return await next(context);
                });
            }
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
