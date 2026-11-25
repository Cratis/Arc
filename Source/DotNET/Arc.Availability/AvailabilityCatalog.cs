// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Holds the 'availability' blocks of a model.
/// </summary>
[IgnoreConvention]
public class AvailabilityCatalog
{
    readonly Dictionary<string, AvailabilityDefinition> _definitions = [];

    /// <summary>
    /// Gets every definition.
    /// </summary>
    public IReadOnlyCollection<AvailabilityDefinition> Definitions => _definitions.Values;

    /// <summary>
    /// Adds a definition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The catalog, for chaining.</returns>
    /// <exception cref="InvalidOperationException">A block with the name is already defined.</exception>
    public AvailabilityCatalog Add(AvailabilityDefinition definition)
    {
        if (!_definitions.TryAdd(definition.Name, definition))
        {
            throw new InvalidOperationException($"The availability '{definition.Name}' is already defined.");
        }

        return this;
    }

    /// <summary>
    /// Gets a definition.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="UndefinedAvailabilityException">No block has the name.</exception>
    public AvailabilityDefinition Get(string name) =>
        _definitions.TryGetValue(name, out var definition) ? definition : throw new UndefinedAvailabilityException(name);

    /// <summary>
    /// Verifies every 'with' names a defined block and no blocks build on each other in a cycle.
    /// </summary>
    /// <exception cref="UndefinedAvailabilityException">A block uses a block that is not defined.</exception>
    /// <exception cref="CircularAvailabilityException">A cycle exists.</exception>
    public void Validate()
    {
        foreach (var definition in _definitions.Values)
        {
            Walk(definition.Expression, [definition.Name]);
        }
    }

    void Walk(AvailabilityExpression expression, List<string> chain)
    {
        switch (expression)
        {
            case AvailableWith with:
                if (chain.Contains(with.Name))
                {
                    throw new CircularAvailabilityException([.. chain, with.Name]);
                }

                chain.Add(with.Name);
                Walk(Get(with.Name).Expression, chain);
                chain.RemoveAt(chain.Count - 1);
                break;
            case AvailabilityAnd and:
                and.Parts.ToList().ForEach(_ => Walk(_, chain));
                break;
            case AvailabilityOr or:
                or.Parts.ToList().ForEach(_ => Walk(_, chain));
                break;
        }
    }
}
