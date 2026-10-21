// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Converts a <see cref="TypeReferenceModel"/> into the Screenplay type reference it corresponds to.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// A concept keeps its own name rather than being reduced to the primitive behind it, because the concept is
/// declared once at the top of the document and referenced by name from there on.
/// </remarks>
public class TypeReferenceConverter(IScreenplayNaming naming)
{
    /// <summary>
    /// The name used when a type reference carries nothing usable.
    /// </summary>
    public const string UnknownTypeName = ScreenplayPrimitiveTypes.String;

    /// <summary>
    /// Converts a type reference.
    /// </summary>
    /// <param name="type">The type reference to convert.</param>
    /// <returns>The <see cref="TypeRefSyntax"/>.</returns>
    public TypeRefSyntax Convert(TypeReferenceModel type)
    {
        var name = naming.ToDeclarationName(type.Name);

        return new(name.Length <= 1 ? UnknownTypeName : name, type.IsCollection, type.IsOptional, SourceLocation.Start);
    }

    /// <summary>
    /// Keeps only properties whose carried types can be declared, reporting each omitted property.
    /// </summary>
    /// <param name="properties">The original properties.</param>
    /// <param name="owner">The declaration owning them.</param>
    /// <param name="diagnostics">Where omissions are reported.</param>
    /// <returns>The representable properties, in declaration order.</returns>
    internal static IReadOnlyList<PropertyModel> Properties(IEnumerable<PropertyModel> properties, string owner, ScreenplayDiagnostics diagnostics) =>
        properties.Where(property =>
        {
            if (property.Type.UnnameableType is not { } original)
            {
                return true;
            }

            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableTypeReference,
                $"Property '{owner}.{property.Name}' of type '{original}' was left out because Screenplay has no type that represents it",
                owner);

            return false;
        }).ToList();
}
