// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Traces;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an instance of <see cref="ICommandFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="ICommandFilter"/> to use for filtering commands.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
[Singleton]
public class CommandFilters(IInstancesOf<ICommandFilter> filters, IActivitySource<CommandFilters> activitySource) : ICommandFilters
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var result = CommandResult.Success(context.CorrelationId);
        using var span = activitySource.OnExecution(context.Type.FullName ?? context.Type.Name);

        foreach (var filter in filters)
        {
            try
            {
                var filterResult = await filter.OnExecution(context);
                if (filterResult is not null)
                {
                    result.MergeWith(filterResult);
                }
            }
            catch (Exception ex)
            {
                // A throwing filter must not abort the chain and discard the verdicts of the filters that already
                // ran (e.g. a clean Unauthorized from an authorization filter). Merge the failure into the running
                // result so prior verdicts are preserved; the short-circuit below then stops the chain. FromException
                // maps an IValidationFailure (invalid client input) to a validation failure (400) and anything else
                // to an error (500).
                result.MergeWith(CommandResult.FromException(context.CorrelationId, ex));
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