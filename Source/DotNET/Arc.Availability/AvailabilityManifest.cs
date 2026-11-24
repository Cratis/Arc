// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// What a model declares about availability, as data. The engine does not read it to evaluate.
/// </summary>
/// <param name="Origin">Where the manifest comes from, for example the model it was emitted from. Empty when unknown.</param>
/// <param name="Audiences">The declared audiences.</param>
/// <param name="Blocks">The 'availability' blocks.</param>
/// <param name="Gates">The gates, one per guarded part of the model.</param>
/// <param name="ContextValues">The context values the model provides.</param>
/// <param name="Personas">The personas the model declares.</param>
public record AvailabilityManifest(
    string Origin,
    IReadOnlyList<AudienceDeclaration> Audiences,
    IReadOnlyList<AvailabilityBlockEntry> Blocks,
    IReadOnlyList<AvailabilityGateEntry> Gates,
    IReadOnlyList<ContextValueEntry> ContextValues,
    IReadOnlyList<PersonaEntry> Personas)
{
    /// <summary>
    /// Gets a manifest that declares nothing.
    /// </summary>
    public static AvailabilityManifest Empty { get; } = new(string.Empty, [], [], [], [], []);
}
