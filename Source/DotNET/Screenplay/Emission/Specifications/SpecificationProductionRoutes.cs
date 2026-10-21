// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Resolves ordered per-production route and destination fixtures without substituting command defaults.
/// </summary>
internal static class SpecificationProductionRoutes
{
    /// <summary>
    /// Completes expected event routing from proven productions and concrete command inputs.
    /// </summary>
    /// <param name="specification">The recovered command scenario.</param>
    /// <param name="command">The issued command.</param>
    /// <returns>The fully routed scenario, or null when its routing cannot be stated.</returns>
    internal static SpecificationModel? Apply(SpecificationModel specification, CommandModel command)
    {
        var events = specification.Then.Where(state => state.Kind == SpecificationStateKind.Event).ToList();
        var productions = command.Produces.ToList();
        if (!events.Select(state => state.Name).SequenceEqual(productions.Select(production => production.EventName), StringComparer.Ordinal))
        {
            return null;
        }
        var completed = new Queue<SpecificationStateModel>();
        foreach (var (state, production) in events.Zip(productions))
        {
            var route = production.Route ?? command.Authoring?.Route;
            var path = production.Destination as PropertyPathSource ??
                ((command.Authoring?.Identifier ?? command.Identifier) is { } identifier ? new PropertyPathSource(identifier) : null);
            var destination = state.For ?? (path is null ? null : Value(path));
            if (destination?.Value is null || route is not { Stream: not null })
            {
                return null;
            }
            var key = route.StreamIdLiteral ?? (route.StreamId is { } property ? Value(new PropertyPathSource(property)) : null);
            var parts = route.StreamIdParts.Select(part => new PropertyMappingModel(part.Name, Value(part.Value) ?? new LiteralSource(null))).ToList();
            if ((route.StreamIdType is not null && key?.Value is null) || parts.Exists(part => part.Source is not LiteralSource { Value: not null }))
            {
                return null;
            }
            completed.Enqueue(state with
            {
                For = destination,
                Route = state.Route ?? SpecificationRouteDeclarations.Inferred(route, key, parts)
            });
        }

        return specification with
        {
            Then = specification.Then.Select(state => state.Kind == SpecificationStateKind.Event ? completed.Dequeue() : state).ToList()
        };

        LiteralSource? Value(MappingSourceModel source) => source switch
        {
            LiteralSource literal => literal,
            PropertyPathSource property => specification.When?.Values.SingleOrDefault(mapping => mapping.Property == property.Path)?.Source as LiteralSource,
            _ => null
        };
    }
}
