// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Http;

/// <summary>
/// Default implementation of <see cref="IHttpRequestContextAccessor"/>.
/// </summary>
/// <remarks>
/// The context is held through a shared holder rather than directly in the <see cref="AsyncLocal{T}"/>. Work that
/// captures the execution context during a request - a reactor dispatched from an append, <c>Task.Run</c>, a timer -
/// keeps its own copy of the async-local value, and clearing the async-local at the end of the request does not reach
/// that copy. Ending the holder does: every execution context captured during the request then sees no request, the
/// way ASP.NET Core's own <c>IHttpContextAccessor</c> behaves. Without it such work reads a request that has already
/// been disposed, and reading its user throws.
/// </remarks>
[Singleton]
public class HttpRequestContextAccessor : IHttpRequestContextAccessor
{
    static readonly AsyncLocal<RequestHolder?> _current = new();

    /// <inheritdoc/>
    public IHttpRequestContext? Current
    {
        get => _current.Value?.Context;
        set => _current.Value = value is null ? null : new RequestHolder(value);
    }

    /// <summary>
    /// Publishes the context of a request for its duration.
    /// </summary>
    /// <param name="accessor">The <see cref="IHttpRequestContextAccessor"/> to publish to.</param>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the request.</param>
    /// <returns>A scope that, when disposed, ends the request for every execution context captured during it.</returns>
    /// <remarks>
    /// An accessor other than this one is assigned to and left as it is, since only this implementation knows how
    /// to end a request for captured execution contexts.
    /// </remarks>
    internal static IDisposable BeginRequest(IHttpRequestContextAccessor accessor, IHttpRequestContext context)
    {
        if (accessor is not HttpRequestContextAccessor)
        {
            accessor.Current = context;
            return NoRequestScope.Instance;
        }

        var holder = new RequestHolder(context);
        _current.Value = holder;
        return holder;
    }

    sealed class RequestHolder(IHttpRequestContext context) : IDisposable
    {
        public IHttpRequestContext? Context { get; private set; } = context;

        public void Dispose() => Context = null;
    }

    sealed class NoRequestScope : IDisposable
    {
        public static readonly NoRequestScope Instance = new();

        public void Dispose()
        {
        }
    }
}
