// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Provides a fixed value: a literal in the model.
/// </summary>
/// <param name="value">The literal.</param>
public class LiteralContextValueProvider(object? value) : IContextValueProvider
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Dependencies => [];

    /// <inheritdoc/>
    public ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(ContextValueOutcome.Present(value));
}
