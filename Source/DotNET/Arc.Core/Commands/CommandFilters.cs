// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an instance of <see cref="ICommandFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="ICommandFilter"/> to use for filtering commands.</param>
[Singleton]
public class CommandFilters(IInstancesOf<ICommandFilter> filters) : ICommandFilters
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var result = CommandResult.Success(context.CorrelationId);

        foreach (var filter in filters)
        {
            var filterResult = await filter.OnExecution(context);
            if (filterResult is not null)
            {
                result.MergeWith(filterResult);
            }
        }

        return result;
    }
}