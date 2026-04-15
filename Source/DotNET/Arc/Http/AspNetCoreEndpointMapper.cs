// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// ASP.NET Core implementation of <see cref="IEndpointMapper"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AspNetCoreEndpointMapper"/> class.
/// </remarks>
/// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
/// <param name="groupPrefix">Optional group prefix for all routes.</param>
public class AspNetCoreEndpointMapper(IEndpointRouteBuilder endpoints, string? groupPrefix = null) : IEndpointMapper
{
    readonly RouteGroupBuilder _group = string.IsNullOrEmpty(groupPrefix)
            ? endpoints.MapGroup(string.Empty)
            : endpoints.MapGroup(groupPrefix);

    /// <inheritdoc/>
    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        Delegate requestHandler = async (HttpContext httpContext) =>
        {
            var context = new AspNetCoreHttpRequestContext(httpContext);
            var accessor = httpContext.RequestServices.GetRequiredService<IHttpRequestContextAccessor>();
            accessor.Current = context;
            await handler(context);
        };

        var builder = _group.MapGet(pattern, requestHandler);

        ApplyMetadata((RouteHandlerBuilder)(object)builder, metadata);
    }

    /// <inheritdoc/>
    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        Delegate requestHandler = async (HttpContext httpContext) =>
        {
            var context = new AspNetCoreHttpRequestContext(httpContext);
            var accessor = httpContext.RequestServices.GetRequiredService<IHttpRequestContextAccessor>();
            accessor.Current = context;
            await handler(context);
        };

        var builder = _group.MapPost(pattern, requestHandler);

        ApplyMetadata((RouteHandlerBuilder)(object)builder, metadata);
    }

    /// <inheritdoc/>
    public bool EndpointExists(string name) => endpoints.EndpointExists(name);

    void ApplyMetadata(RouteHandlerBuilder builder, EndpointMetadata? metadata)
    {
        if (metadata is null) return;

        builder.WithName(metadata.Name);

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
