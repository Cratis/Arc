// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Attaches the rules a validator declares for a concept to the concept itself.
/// </summary>
/// <remarks>
/// A concept validator is written against the concept's value, and a concept declaration takes its rules against
/// its own implied value, so the two line up exactly - the property the rule names is dropped by emission.
/// </remarks>
public static class ConceptValidations
{
    /// <summary>
    /// Attaches every concept validator's rules to the concept it validates.
    /// </summary>
    /// <param name="catalog">The catalogue of everything the compilation declares.</param>
    /// <param name="readers">The readers holding the concepts and the validators.</param>
    public static void Link(ArtifactCatalog catalog, ArtifactReaders readers)
    {
        var declared = readers.Types.Concepts.Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var type in catalog.Types.Where(_ => declared.Contains(_.Name)))
        {
            if (type.FindBase(WellKnownTypeNames.ConceptAs) is null)
            {
                continue;
            }

            readers.Types.AddValidations(type.Name, readers.Validators.For(type));
        }
    }
}
