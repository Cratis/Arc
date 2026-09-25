// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the durable request identity and tenant-relevant data captured when an observable query is subscribed.
/// </summary>
/// <param name="requestContext">The subscribe request whose identity and request data are captured.</param>
/// <param name="transportContext">The long-lived context used only for response transport operations.</param>
/// <param name="requestServices">A durable service provider, not the subscribe request's disposed scope.</param>
/// <param name="requestAborted">The subscription cancellation token.</param>
/// <remarks>
/// Request data is copied from the subscribe request and is therefore independent of that request's lifetime. Response
/// operations are forwarded only to the long-lived transport context.
/// </remarks>
internal sealed class ObservableQuerySubscriptionHttpRequestContext(
    IHttpRequestContext requestContext,
    IHttpRequestContext transportContext,
    IServiceProvider requestServices,
    CancellationToken requestAborted) : IHttpRequestContext, IAuthorizationRequestContext
{
    readonly IHttpRequestContext _transportContext = transportContext;
    ClaimsPrincipal _user = ClonePrincipal(requestContext.User);
    IServiceProvider _requestServices = requestServices;
    TenantId? _emissionTenant;
    Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? _nativeRequest;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Query { get; } = Snapshot(requestContext.Query);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Headers { get; } = Snapshot(requestContext.Headers);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Cookies { get; } = Snapshot(requestContext.Cookies);

    /// <inheritdoc/>
    public string Host { get; } = requestContext.Host ?? string.Empty;

    /// <inheritdoc/>
    public string Path { get; } = requestContext.Path ?? string.Empty;

    /// <inheritdoc/>
    public string Method { get; } = requestContext.Method ?? string.Empty;

    /// <inheritdoc/>
    public IServiceProvider RequestServices => _requestServices;

    /// <inheritdoc/>
    public CancellationToken RequestAborted { get; } = requestAborted;

    /// <inheritdoc/>
    public IWebSocketContext WebSockets => _transportContext.WebSockets;

    /// <inheritdoc/>
    /// <remarks>
    /// Reads return an isolated clone of the one frozen snapshot. This preserves the mutable
    /// <see cref="IHttpRequestContext"/> contract for filters without allowing a filter to mutate or replace the
    /// identity later used by ambient emissions and <see cref="ObservableQueryEmissionContext"/>. Assignments are
    /// deliberately ignored for the same reason.
    /// </remarks>
    public ClaimsPrincipal User
    {
        get => ClonePrincipal(_user);
        set
        {
            // IHttpRequestContext requires a setter. A long-lived subscription cannot safely replace its captured
            // identity after authorization because emission guards must observe the exact same snapshot.
        }
    }

    /// <inheritdoc/>
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>(requestContext.Items ?? new Dictionary<object, object?>());

    /// <inheritdoc/>
    public bool IsHttps { get; } = requestContext.IsHttps;

    /// <inheritdoc/>
    public string? ContentType
    {
        get => _transportContext.ContentType;
        set => _transportContext.ContentType = value;
    }

    /// <inheritdoc/>
    public int StatusCode
    {
        get => _transportContext.StatusCode;
        set => _transportContext.StatusCode = value;
    }

    /// <inheritdoc/>
    public Task<object?> ReadBodyAsJson(Type type, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The observable query subscription context does not retain a request body.");

    /// <inheritdoc/>
    public void SetStatusCode(int statusCode) => _transportContext.SetStatusCode(statusCode);

    /// <inheritdoc/>
    public void SetResponseHeader(string name, string value) => _transportContext.SetResponseHeader(name, value);

    /// <inheritdoc/>
    public string? GetResponseHeader(string name) => _transportContext.GetResponseHeader(name);

    /// <inheritdoc/>
    public void AppendCookie(string key, string value, CookieOptions options) => _transportContext.AppendCookie(key, value, options);

    /// <inheritdoc/>
    public void RemoveCookie(string key) => _transportContext.RemoveCookie(key);

    /// <inheritdoc/>
    public Task Write(string text, CancellationToken cancellationToken = default) => _transportContext.Write(text, cancellationToken);

    /// <inheritdoc/>
    public Task WriteBytes(byte[] data, CancellationToken cancellationToken = default) => _transportContext.WriteBytes(data, cancellationToken);

    /// <inheritdoc/>
    public Task WriteStream(Stream stream, CancellationToken cancellationToken = default) => _transportContext.WriteStream(stream, cancellationToken);

    /// <inheritdoc/>
    public Task WriteResponseAsJson(object? value, Type type, CancellationToken cancellationToken = default) =>
        _transportContext.WriteResponseAsJson(value, type, cancellationToken);

    /// <inheritdoc/>
    IDisposable IAuthorizationRequestContext.BeginSelectedPrincipal(ClaimsPrincipal principal, IServiceProvider services) =>
        BeginSelectedPrincipal(principal, services);

    /// <summary>
    /// Gets the frozen principal snapshot used by the explicit emission context.
    /// </summary>
    /// <returns>An isolated clone of the principal snapshot.</returns>
    internal ClaimsPrincipal GetPrincipal() => ClonePrincipal(_user);

    /// <summary>
    /// Freezes the identity selected by authorization before the subscription begins emitting.
    /// </summary>
    /// <param name="principal">The authenticated scheme principal.</param>
    /// <param name="services">The owned execution provider retained until the subscription ends, when selected.</param>
    internal void SelectAuthorizedPrincipal(ClaimsPrincipal principal, IServiceProvider? services = null)
    {
        _user = ClonePrincipal(principal);
        if (services is not null)
        {
            _requestServices = services;
        }
    }

    /// <summary>
    /// Temporarily exposes the selected principal to tenant resolution during subscription admission.
    /// </summary>
    /// <param name="principal">The selected principal.</param>
    /// <param name="services">The clean execution services for this subscription.</param>
    /// <returns>A scope restoring the original subscription identity before the final snapshot is chosen.</returns>
    internal IDisposable BeginSelectedPrincipal(ClaimsPrincipal principal, IServiceProvider services)
    {
        var previous = _user;
        var previousServices = _requestServices;
        _user = ClonePrincipal(principal);
        _requestServices = services;
        return new SelectedPrincipalScope(this, previous, previousServices);
    }

    /// <summary>Captures the direct stream's selected tenant and live native request scope.</summary>
    /// <param name="tenant">The selected tenant.</param>
    /// <param name="nativeRequest">A scope factory limited to this live HTTP request.</param>
    internal void ConfigureEmission(TenantId tenant, Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? nativeRequest)
    {
        _emissionTenant = tenant;
        _nativeRequest = nativeRequest;
    }

    /// <summary>Restores the subscriber's identity around a direct stream emission.</summary>
    /// <returns>An operation-local identity scope.</returns>
    internal IDisposable BeginEmission() => ObservableEmissionIdentity.Begin(this, RequestServices, GetPrincipal(), _emissionTenant, _nativeRequest);

    static ReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string>? values) =>
        new(new Dictionary<string, string>(values ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));

    static ClaimsPrincipal ClonePrincipal(ClaimsPrincipal? principal) =>
        principal is null
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone()));

    sealed class SelectedPrincipalScope(ObservableQuerySubscriptionHttpRequestContext context, ClaimsPrincipal previous, IServiceProvider previousServices) : IDisposable
    {
        public void Dispose()
        {
            context._requestServices = previousServices;
            context._user = previous;
        }
    }
}
