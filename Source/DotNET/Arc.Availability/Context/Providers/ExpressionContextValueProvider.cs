// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Provides a value taken from another context value by path, for example 'identity.roles'.
/// </summary>
/// <param name="path">The path to read.</param>
public class ExpressionContextValueProvider(string path) : IContextValueProvider
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Dependencies { get; } =
        [(path.StartsWith("$context.", StringComparison.Ordinal) ? path["$context.".Length..] : path).Split('.')[0]];

    /// <inheritdoc/>
    public ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken) =>
        context.Resolve(path, cancellationToken);
}
