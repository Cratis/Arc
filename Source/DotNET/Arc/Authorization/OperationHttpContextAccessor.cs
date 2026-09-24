// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Overlays the host's HTTP accessor per asynchronous operation without changing the shared connection context.
/// Outside an Arc identity boundary the original accessor and HTTP context retain their exact behavior.
/// </summary>
/// <param name="inner">The application's original HTTP context accessor.</param>
internal sealed class OperationHttpContextAccessor(IHttpContextAccessor inner) : IHttpContextAccessor
{
    static readonly AsyncLocal<Frame?> _frame = new();

    /// <inheritdoc/>
    public HttpContext? HttpContext
    {
        get => _frame.Value is { } frame ? frame.Context : inner.HttpContext;
        set
        {
            if (_frame.Value is { } frame)
            {
                _frame.Value = new Frame(value, frame.Previous);
            }
            else
            {
                inner.HttpContext = value;
            }
        }
    }

    /// <summary>Starts a selected HTTP identity without modifying the shared request object.</summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The execution provider.</param>
    /// <returns>The operation-local HTTP scope.</returns>
    internal IDisposable Begin(ClaimsPrincipal principal, IServiceProvider services) => Begin(principal, services, HttpContext);

    /// <summary>Builds an isolated HTTP facade from a captured, still-live direct request.</summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The emission provider.</param>
    /// <param name="original">The live direct request, or null when unavailable.</param>
    /// <returns>The operation-local HTTP scope.</returns>
    internal IDisposable Begin(ClaimsPrincipal principal, IServiceProvider services, HttpContext? original)
    {
        if (original is null)
        {
            return BeginFrame(null);
        }

        // A new feature collection shadows only identity and services. All unrelated HTTP features (including
        // response and transport features) continue to come from the live context, without touching its User or
        // RequestServices, even when admissions and emissions overlap on the same socket.
        var features = new FeatureCollection(original.Features);
        features.Set<IHttpAuthenticationFeature>(new UserFeature { User = principal });
        features.Set<IServiceProvidersFeature>(new ServicesFeature(services));
        return BeginFrame(new DefaultHttpContext(features));
    }

    /// <summary>Hides a disposed or unrelated native request from a background emission.</summary>
    /// <returns>The operation-local HTTP scope.</returns>
    internal IDisposable Suppress() => BeginFrame(null);

    Frame BeginFrame(HttpContext? context)
    {
        var frame = new Frame(context, _frame.Value);
        _frame.Value = frame;
        return frame;
    }

    sealed class UserFeature : IHttpAuthenticationFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }

    sealed class ServicesFeature(IServiceProvider services) : IServiceProvidersFeature
    {
        public IServiceProvider RequestServices { get; set; } = services;
    }

    sealed class Frame(HttpContext? context, Frame? previous) : IDisposable
    {
        public HttpContext? Context { get; } = context;

        public Frame? Previous { get; } = previous;

        public void Dispose() => _frame.Value = Previous;
    }
}
