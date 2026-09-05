// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;

namespace Cratis.Arc.Queries;

/// <summary>
/// Captures what identifies a multiplexed observable query subscription for the lifetime of its stream.
/// </summary>
/// <param name="queryName">The query name.</param>
/// <param name="arguments">The coerced arguments to capture.</param>
/// <param name="principal">The caller that established the subscription.</param>
/// <param name="serializerOptions">The Arc JSON serializer options.</param>
/// <remarks>
/// Emissions arrive on the producing stream's own thread, where the request's <c>AsyncLocal</c> context does not flow,
/// so anything an emission needs about the caller has to be captured at subscribe time and carried explicitly.
/// Query arguments are serialized immediately so later request or query-context mutation cannot change the identity.
/// </remarks>
internal sealed class ObservableQuerySubscriptionIdentity(
    FullyQualifiedQueryName queryName,
    QueryArguments arguments,
    ClaimsPrincipal? principal,
    JsonSerializerOptions serializerOptions)
{
    readonly ObservableQueryArgumentsSnapshot _arguments = new(arguments, serializerOptions);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQuerySubscriptionIdentity"/> class with Arc's default serializer options.
    /// </summary>
    /// <param name="queryName">The query name.</param>
    /// <param name="arguments">The coerced arguments to capture.</param>
    /// <param name="principal">The caller that established the subscription.</param>
    public ObservableQuerySubscriptionIdentity(
        FullyQualifiedQueryName queryName,
        QueryArguments arguments,
        ClaimsPrincipal? principal)
        : this(queryName, arguments, principal, new ArcOptions().JsonSerializerOptions)
    {
    }

    /// <summary>
    /// Gets the query name.
    /// </summary>
    public FullyQualifiedQueryName QueryName { get; } = queryName;

    /// <summary>
    /// Gets the caller that established the subscription.
    /// </summary>
    public ClaimsPrincipal? Principal { get; } = principal;

    /// <summary>
    /// Creates independent query arguments from the immutable subscription baseline.
    /// </summary>
    /// <returns>A deep clone of the subscription arguments.</returns>
    public QueryArguments CreateArguments() => _arguments.CreateArguments();
}
