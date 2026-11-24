// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A gate in the manifest: the 'available' lines of one part of the model.
/// </summary>
/// <param name="Target">The part of the model the gate guards, for example 'Onboarding/Vessels/AddVessel'.</param>
/// <param name="Alternatives">The gate in disjunctive normal form, with 'with' expanded. Any one alternative opens it.</param>
public record AvailabilityGateEntry(string Target, IReadOnlyList<AvailabilityAlternative> Alternatives)
{
    /// <summary>
    /// Creates the entry for an expression, expanding it against a catalog.
    /// </summary>
    /// <param name="target">The part of the model the gate guards.</param>
    /// <param name="expression">The gate.</param>
    /// <param name="catalog">The catalog holding every block the gate uses.</param>
    /// <returns>The entry.</returns>
    public static AvailabilityGateEntry From(string target, AvailabilityExpression expression, AvailabilityCatalog catalog) =>
        new(target, expression.Alternatives(catalog));
}
