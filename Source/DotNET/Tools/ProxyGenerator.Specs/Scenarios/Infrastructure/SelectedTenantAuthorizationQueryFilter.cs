// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Captures the tenant bound before an authorization query filter is constructed.
/// </summary>
public class SelectedTenantAuthorizationQueryFilter : IAuthorizationQueryFilter
{
    readonly TenantBoundService _bound;
    readonly SelectedAuthorizationQueryFilterObservations _observations;

    /// <summary>Captures tenant binding before authorization begins.</summary>
    /// <param name="bound">A scoped tenant-bound dependency.</param>
    /// <param name="observations">The constructor and invocation observations.</param>
    public SelectedTenantAuthorizationQueryFilter(TenantBoundService bound, SelectedAuthorizationQueryFilterObservations observations)
    {
        _bound = bound;
        _observations = observations;
        observations.RecordConstruction(bound.Tenant.Value, bound.Id);
    }

    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Name.Value == $"{typeof(Scenarios.for_Queries.ModelBound.PolicyProtectedReadModel).FullName}.All")
        {
            _observations.RecordExecution(_bound.Tenant.Value);
        }

        return Task.FromResult(QueryResult.Success(context.CorrelationId));
    }
}
