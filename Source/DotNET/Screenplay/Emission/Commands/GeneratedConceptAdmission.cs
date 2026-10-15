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
    public static bool Allows(PropertyModel property, CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? maximumVersion)
    {
        if (property.Type.IsOptional || property.Type.IsCollection || model.Concepts.SingleOrDefault(concept => concept.Name == property.Type.Name) is not { } concept)
        {
            return false;
        }
        if ((concept.Generation is null && concept.Primitive != ScreenplayPrimitive.Uuid) || GenerationAdmission.Failure(concept, maximumVersion) is not null)
        {
            return false;
        }

        return property.Name != authoring.Identifier || RandomBits(concept.Generation) >= 64;
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
