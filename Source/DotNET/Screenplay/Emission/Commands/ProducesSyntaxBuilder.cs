// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Builds the Screenplay <c>produces</c> blocks of a command.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class ProducesSyntaxBuilder(IScreenplayNaming naming)
{
    readonly MappingSourceConverter _sources = new(naming);
    readonly ConditionConverter _conditions = new(naming);

    /// <summary>
    /// Builds the produces blocks of a command.
    /// </summary>
    /// <param name="produces">The events the command produces.</param>
    /// <returns>The produces blocks, in the order the command declares them.</returns>
    public IEnumerable<ProducesSyntax> Build(IEnumerable<ProducesModel> produces) => [.. produces.Select(Build)];

    /// <summary>
    /// Builds a single produces block.
    /// </summary>
    /// <param name="produces">The event production to build for.</param>
    /// <returns>The <see cref="ProducesSyntax"/>.</returns>
    ProducesSyntax Build(ProducesModel produces) =>
        new(
            naming.ToDeclarationName(produces.EventName),
            _conditions.Convert(produces.When),
            [.. produces.Mappings.Select(ToMapping)],
            SourceLocation.Start);

    /// <summary>
    /// Converts a single mapping onto an event property.
    /// </summary>
    /// <param name="mapping">The mapping to convert.</param>
    /// <returns>The <see cref="PropertyMappingSyntax"/>.</returns>
    PropertyMappingSyntax ToMapping(PropertyMappingModel mapping) =>
        new(naming.ToPropertyName(mapping.Property), _sources.Convert(mapping.Source), SourceLocation.Start);
}
