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
/// <param name="names">The <see cref="NameAvailability"/> deciding which properties the block can map onto.</param>
public class ProducesSyntaxBuilder(IScreenplayNaming naming, NameAvailability names)
{
    readonly MappingSourceConverter _sources = new(naming);
    readonly ConditionConverter _conditions = new(naming);

    /// <summary>
    /// Builds the produces blocks of a command.
    /// </summary>
    /// <param name="produces">The events the command produces.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The produces blocks, in the order the command declares them.</returns>
    public IEnumerable<ProducesSyntax> Build(IEnumerable<ProducesModel> produces, string location) =>
        [.. produces.Select(_ => Build(_, location))];

    /// <summary>
    /// Builds a single produces block.
    /// </summary>
    /// <param name="produces">The event production to build for.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProducesSyntax"/>.</returns>
    /// <remarks>
    /// A mapping is written onto the property of the event it fills in, so a mapping onto a property the block reads
    /// as a directive of its own is left out for the same reason the property itself is.
    /// </remarks>
    ProducesSyntax Build(ProducesModel produces, string location) =>
        new(
            naming.ToDeclarationName(produces.EventName),
            _conditions.Convert(produces.When),
            [
                .. produces.Mappings
                    .Where(_ => names.Allows(_.Property, ReservedWords.InProduces, produces.EventName, location))
                    .Select(ToMapping)
            ],
            SourceLocation.Start);

    /// <summary>
    /// Converts a single mapping onto an event property.
    /// </summary>
    /// <param name="mapping">The mapping to convert.</param>
    /// <returns>The <see cref="PropertyMappingSyntax"/>.</returns>
    PropertyMappingSyntax ToMapping(PropertyMappingModel mapping) =>
        new(naming.ToPropertyName(mapping.Property), _sources.Convert(mapping.Source), SourceLocation.Start);
}
