// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// An 'availability' block in the manifest.
/// </summary>
/// <param name="Name">The name of the block.</param>
/// <param name="Description">What the block is for. Empty when the model does not say.</param>
/// <param name="Source">Where in the model it is declared. Empty when unknown.</param>
/// <param name="Alternatives">The block in disjunctive normal form, with 'with' expanded.</param>
public record AvailabilityBlockEntry(string Name, string Description, string Source, IReadOnlyList<AvailabilityAlternative> Alternatives)
{
    /// <summary>
    /// Creates the entry for a definition, expanding it against a catalog.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <param name="catalog">The catalog holding every block the definition uses.</param>
    /// <param name="source">Where in the model it is declared.</param>
    /// <returns>The entry.</returns>
    public static AvailabilityBlockEntry From(AvailabilityDefinition definition, AvailabilityCatalog catalog, string source = "") =>
        new(definition.Name, definition.Description, source, definition.Expression.Alternatives(catalog));
}
