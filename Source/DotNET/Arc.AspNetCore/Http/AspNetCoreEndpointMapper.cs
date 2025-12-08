// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// ASP.NET Core implementation of <see cref="IEndpointMapper"/>.
/// </summary>
public class AspNetCoreEndpointMapper : IEndpointMapper
{
    readonly IEndpointRouteBuilder _endpoints;
    readonly RouteGroupBuilder _group;
    readonly HashSet<string> _existingEndpoints = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetCoreEndpointMapper"/> class.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="groupPrefix">Optional group prefix for all routes.</param>
    public AspNetCoreEndpointMapper(IEndpointRouteBuilder endpoints, string? groupPrefix = null)
    {
        _endpoints = endpoints;
        _group = string.IsNullOrEmpty(groupPrefix)
            ? endpoints.MapGroup(string.Empty)
            : endpoints.MapGroup(groupPrefix);
    }

    /// <inheritdoc/>
    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        var builder = _group.MapGet(pattern, async (HttpContext httpContext) =>
        {
            var context = new AspNetCoreHttpRequestContext(httpContext);
            await handler(context);
        });

        ApplyMetadata((RouteHandlerBuilder)(object)builder, metadata);
    }

    /// <inheritdoc/>
    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        var builder = _group.MapPost(pattern, async (HttpContext httpContext) =>
        {
            var context = new AspNetCoreHttpRequestContext(httpContext);
            await handler(context);
        });

        ApplyMetadata((RouteHandlerBuilder)(object)builder, metadata);
    }

    /// <inheritdoc/>
    public bool EndpointExists(string name)
    {
        return _existingEndpoints.Contains(name);
    }

    void ApplyMetadata(RouteHandlerBuilder builder, EndpointMetadata? metadata)
    {
        if (metadata is null) return;

        builder.WithName(metadata.Name);
        _existingEndpoints.Add(metadata.Name);

        if (!string.IsNullOrEmpty(metadata.Summary))
        {
            builder.WithSummary(metadata.Summary);
        }

        if (metadata.Tags is not null && metadata.Tags.Any())
        {
            builder.WithTags(metadata.Tags.ToArray());
        }

        if (metadata.AllowAnonymous)
        {
            builder.AllowAnonymous();
        }
    }
}
