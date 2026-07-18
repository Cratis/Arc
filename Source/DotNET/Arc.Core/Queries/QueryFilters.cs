// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Traces;
using Cratis.Types;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an instance of <see cref="IQueryFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="IQueryFilter"/> to use for filtering queries.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
[Singleton]
public class QueryFilters(IInstancesOf<IQueryFilter> filters, IActivitySource<QueryFilters> activitySource) : IQueryFilters
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        var result = QueryResult.Success(context.CorrelationId);
        using var span = activitySource.OnPerform(context.Name.Value);

        // Evaluate authorization filters before ordinary filters, independent of the order IInstancesOf yields them.
        // Without this the short-circuit below could return on a validation failure before the authorization filter
        // runs — turning what should be a 403 for a forbidden caller into a 400 depending on undefined discovery order.
        // OrderBy is a stable sort, so filters within the same group keep their discovery order.
        foreach (var filter in filters.OrderBy(filter => filter is IAuthorizationQueryFilter ? 0 : 1))
        {
            try
            {
                var filterResult = await filter.OnPerform(context);
                if (filterResult is not null)
                {
                    result.MergeWith(filterResult);
                }
            }
            catch (Exception ex)
            {
                // A throwing filter must not abort the chain and discard the verdicts of the filters that already
                // ran (e.g. a clean Unauthorized from an authorization filter) by surfacing as an unhandled 500.
                // Merge the failure into the running result so prior verdicts are preserved; the short-circuit below
                // then stops the chain. FromException maps an IValidationFailure (invalid client input) to a
                // validation failure (400) and anything else to an error (500).
                result.MergeWith(QueryResult.FromException(context.CorrelationId, ex));
            }

            // Stop once a filter has produced a non-success (blocking) verdict — an authorization denial or a
            // validation failure must not be overwritten (or a later filter allowed to throw) by continuing the chain.
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return result;
    }
}