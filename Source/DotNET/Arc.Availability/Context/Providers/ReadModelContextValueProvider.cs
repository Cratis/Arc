// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Provides a value by running a read model query keyed by another context value.
/// </summary>
/// <remarks>
/// A key that cannot be resolved, or a query with no matching row, is absent. A store failure throws.
/// </remarks>
/// <param name="query">How queries are run.</param>
/// <param name="queryName">The name of the query.</param>
/// <param name="keyPath">The context path the key is read from, for example 'arguments.vesselId'.</param>
public class ReadModelContextValueProvider(IReadModelQuery query, string queryName, string keyPath) : IContextValueProvider
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Dependencies { get; } =
        [(keyPath.StartsWith("$context.", StringComparison.Ordinal) ? keyPath["$context.".Length..] : keyPath).Split('.')[0]];

    /// <inheritdoc/>
    public async ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken)
    {
        var key = await context.Resolve(keyPath, cancellationToken);
        if (!key.IsPresent)
        {
            return ContextValueOutcome.Absent;
        }

        return ContextValueOutcome.Present(await query.Execute(queryName, key.Value!, cancellationToken));
    }
}
