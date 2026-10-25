// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// Extension methods for carrying the <see cref="EndpointGroupScope"/> of a request on its <see cref="IHttpRequestContext"/>.
/// </summary>
public static class EndpointGroupScopeHttpRequestContextExtensions
{
    static readonly object _itemKey = new();

    /// <summary>
    /// Gets the <see cref="EndpointGroupScope"/> of a request.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <returns>The scope the host set, or <see cref="EndpointGroupScope.Default"/> when it set none.</returns>
    public static EndpointGroupScope GetEndpointGroupScope(this IHttpRequestContext context) =>
        context.Items.TryGetValue(_itemKey, out var scope) && scope is EndpointGroupScope endpointGroupScope
            ? endpointGroupScope
            : EndpointGroupScope.Default;

    /// <summary>
    /// Sets the <see cref="EndpointGroupScope"/> of a request. Only the host does this, from the local port of the connection.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <param name="scope">The <see cref="EndpointGroupScope"/> to set.</param>
    public static void SetEndpointGroupScope(this IHttpRequestContext context, EndpointGroupScope scope) =>
        context.Items[_itemKey] = scope;
}
