// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Verifies that an ordinary filter's scoped constructor dependency is built only after scheme authorization.
/// </summary>
/// <param name="bound">The tenant-bound scoped dependency.</param>
/// <param name="observations">The test observations.</param>
public class SelectedTenantOrdinaryQueryFilter(TenantBoundService bound, SelectedQueryFilterObservations observations) : IQueryFilter
{
    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Name.Value == $"{typeof(Scenarios.for_Queries.ModelBound.PolicyProtectedReadModel).FullName}.All")
        {
            observations.Record(bound.Tenant.Value);
        }

        return Task.FromResult(QueryResult.Success(context.CorrelationId));
    }
}
