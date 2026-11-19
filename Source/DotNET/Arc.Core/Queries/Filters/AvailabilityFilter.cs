// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that refuses a query in a part that is not available, before authorization.
/// </summary>
/// <remarks>
/// The decision comes from the registered <see cref="IAvailabilityGate"/>. Without one the filter does nothing. An
/// unavailable or unknown decision is refused with <see cref="AvailabilityReason.NotAvailable"/>; a failure to decide
/// is a retryable error, never a denial.
/// </remarks>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> used to find the part the query belongs to.</param>
public class AvailabilityFilter(IQueryPerformerProviders queryPerformerProviders) : IAvailabilityQueryFilter
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var gate = context.ServiceProvider?.GetService<IAvailabilityGate>();
        if (gate is null ||
            context.ServiceProvider is null ||
            !queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer) ||
            !gate.IsGated(performer.Type))
        {
            return QueryResult.Success(context.CorrelationId);
        }

        try
        {
            var decision = await gate.Decide(performer.Type, context.ServiceProvider, context.CancellationToken);
            return decision.IsAvailable
                ? QueryResult.Success(context.CorrelationId)
                : QueryResult.Unauthorized(context.CorrelationId, AvailabilityReason.NotAvailable);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.ServiceProvider.GetService<ILogger<AvailabilityFilter>>()?.AvailabilityNotDecided(context.Name.Value, ex);
            return QueryResult.FromException(context.CorrelationId, new AvailabilityNotDecidedException(ex));
        }
    }
}
