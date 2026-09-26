// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Execution;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the context for a query.
/// </summary>
/// <param name="Name">The name of the query.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the query.</param>
/// <param name="Paging">The <see cref="Paging"/> information.</param>
/// <param name="Sorting">The <see cref="Sorting"/> information.</param>
/// <param name="Arguments">Optional arguments for the query.</param>
/// <param name="Dependencies">Optional dependencies required to handle the query.</param>
/// <param name="ServiceProvider">The <see cref="IServiceProvider"/> scoped to the query, used to resolve scoped collaborators such as validators during the query's lifetime.</param>
/// <param name="CancellationToken">The cancellation token for the query, so a cancelled request is not mistaken for invalid input.</param>
public record QueryContext(FullyQualifiedQueryName Name, CorrelationId CorrelationId, Paging Paging, Sorting Sorting, QueryArguments? Arguments = null, IEnumerable<object>? Dependencies = null, IServiceProvider? ServiceProvider = null, CancellationToken CancellationToken = default)
{
    /// <summary>
    /// Represents a query context that is not set.
    /// </summary>
    public static readonly QueryContext NotSet = new("[NotSet]", CorrelationId.NotSet, Paging.NotPaged, Sorting.None);

    /// <summary>
    /// Gets the time Arc received this query operation. Model-bound dispatch captures it before binding and authorization preparation;
    /// MVC action filters capture it after MVC binding. This is neither network arrival nor time before application middleware.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// Gets or sets the total number of items in the query.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets the principal selected during authorization, for a long-lived subscription's identity snapshot.
    /// </summary>
    public ClaimsPrincipal? AuthorizedPrincipal { get; internal set; }

    /// <summary>
    /// Gets the operation-local authorization plan prepared by a host before creating a fresh execution scope.
    /// </summary>
    internal PreparedAuthorization? PreparedAuthorization { get; set; }

    /// <summary>Gets or sets the captured tenant used after the direct query pipeline returns.</summary>
    internal TenantId? EmissionTenant { get; set; }

    /// <summary>Gets or sets the still-live direct request's native scope factory.</summary>
    internal Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? NativeEmissionRequest { get; set; }
}
