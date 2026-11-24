// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Provides a value computed by code. Code is opaque: list the values it reads in <paramref name="dependencies"/> so cycles are found early.
/// </summary>
/// <param name="code">The code. Return <see langword="null"/> for an absent value.</param>
/// <param name="dependencies">The names of the context values the code reads.</param>
public class CodeContextValueProvider(
    Func<IContextValueResolver, CancellationToken, ValueTask<object?>> code,
    IReadOnlyCollection<string>? dependencies = null) : IContextValueProvider
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Dependencies { get; } = dependencies ?? [];

    /// <inheritdoc/>
    public async ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken) =>
        ContextValueOutcome.Present(await code(context, cancellationToken));
}
