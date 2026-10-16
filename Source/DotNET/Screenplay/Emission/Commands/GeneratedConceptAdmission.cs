// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Admits recovered generation without relaxing rule, shape or version boundaries.
/// </summary>
internal static class GeneratedConceptAdmission
{
    /// <summary>
    /// Checks whether a generated property has an executable concept contract.
    /// </summary>
    /// <param name="property">The generated property.</param>
    /// <param name="authoring">The recovered command intent.</param>
    /// <param name="model">The application's concept declarations.</param>
    /// <param name="maximumVersion">The requested executable version cap.</param>
    /// <returns>Whether generation may be emitted.</returns>
    public static bool Allows(PropertyModel property, CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? maximumVersion) => Failure(property, authoring, model, maximumVersion) is null;

    /// <summary>
    /// Returns the first ordinary or identifier-specific refusal without probing an inadmissible contract.
    /// </summary>
    /// <param name="property">The generated value.</param>
    /// <param name="authoring">The command identifying its destination.</param>
    /// <param name="model">The recovered concepts.</param>
    /// <param name="cap">The executable version boundary.</param>
    /// <returns>A nonempty refusal, or null for an admitted value.</returns>
    internal static string? Failure(PropertyModel property, CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? cap)
    {
        var concept = ConceptOf(property, model);
        if (concept is null)
        {
            return "not a required scalar concept";
        }
        if (!HasGeneration(concept))
        {
            return "the concept has no recovered generation contract";
        }
        return GenerationAdmission.Failure(concept, cap) ?? (property.Name == authoring.Identifier ? IdentifierFailure(concept) : null);
    }

    static ConceptModel? ConceptOf(PropertyModel property, ApplicationModel model) => property.Type.IsOptional || property.Type.IsCollection
        ? null : model.Concepts.SingleOrDefault(concept => concept.Name == property.Type.Name);

    static bool HasGeneration(ConceptModel concept) => concept.Generation is not null || concept.Primitive == ScreenplayPrimitive.Uuid;

    static string? IdentifierFailure(ConceptModel concept)
    {
        if (concept.Generation?.Kind == "rule")
        {
            return "opaque rule bodies have unknown identifier entropy";
        }
        if (concept.Generation?.RequiresV21 == true)
        {
            return GenerationAdmission.CheckRules(concept, identifier: true);
        }

        return RandomBits(concept.Generation) >= 64 ? null : "generated identifiers require at least 64 random bits";
    }

    static double RandomBits(GenerationModel? generation) => generation?.Kind switch
    {
        null or "uuid" => 122,
        "uuidv7" or "typeid" => 74,
        "ulid" => 80,
        "nanoid" => (generation.Size ?? 21) * Math.Log2((generation.Alphabet ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-").Length),
        "bytes" => 8 * generation.Count!.Value,
        _ => 0
    };
}
