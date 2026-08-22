// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Security.Claims;
using Cratis.Arc.Http;

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
    CancellationToken requestAborted) : IHttpRequestContext
{
    readonly IHttpRequestContext _transportContext = transportContext;
    readonly ClaimsPrincipal _user = ClonePrincipal(requestContext.User);

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
    public IServiceProvider RequestServices { get; } = requestServices;

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

    /// <summary>
    /// Gets the frozen principal snapshot used by the explicit emission context.
    /// </summary>
    /// <returns>The principal snapshot.</returns>
    internal ClaimsPrincipal GetPrincipal() => _user;

    static ReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string>? values) =>
        new(new Dictionary<string, string>(values ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase));

    static ClaimsPrincipal ClonePrincipal(ClaimsPrincipal? principal) =>
        principal is null
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone()));
}
