// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IMiddleware"/> that publishes the current <see cref="IHttpRequestContext"/> to the
/// <see cref="IHttpRequestContextAccessor"/> for the whole request.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> to publish the context to.</param>
/// <remarks>
/// The built-in tenant resolvers — and anything else that reads the request — go through
/// <see cref="IHttpRequestContextAccessor"/>. Assigning it only inside individual endpoint delegates leaves it unset
/// for work that runs earlier in the request, such as identity resolution that reads tenant-scoped data. That work
/// then sees no request context and silently resolves no tenant. Publishing the context in early middleware makes it
/// available for the entire pipeline, so tenant resolution is consistent regardless of where in the request it runs.
/// </remarks>
public class HttpRequestContextMiddleware(IHttpRequestContextAccessor httpRequestContextAccessor) : IMiddleware
{
    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        httpRequestContextAccessor.Current = new AspNetCoreHttpRequestContext(context);
        await next(context);
    }
}
