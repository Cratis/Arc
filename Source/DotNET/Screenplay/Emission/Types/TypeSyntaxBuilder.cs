// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Builds the document level <c>type</c> declarations.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The <see cref="TypeReferenceConverter"/> used for property types.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything left out is reported to.</param>
/// <remarks>
/// A declaration name is what two names finally become the same or stay apart under, so the last word on whether two
/// shapes can both be declared is here rather than where they were collected. Two records the source tells apart by
/// namespace, and a shape whose name a concept was written under, both come out of naming as one word - and a
/// document declaring that word twice does not compile.
/// </remarks>
public class TypeSyntaxBuilder(IScreenplayNaming naming, TypeReferenceConverter types, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Builds every shape the document declares.
    /// </summary>
    /// <param name="models">The shapes to build.</param>
    /// <param name="concepts">The concepts already declared, whose names a shape cannot be declared under.</param>
    /// <param name="location">Where to report anything left out against.</param>
    /// <returns>The type declarations, ordered by name.</returns>
    public IEnumerable<TypeSyntax> Build(IEnumerable<TypeModel> models, IEnumerable<ConceptSyntax> concepts, string? location)
    {
        var declared = new Dictionary<string, TypeSyntax>(StringComparer.Ordinal);
        var taken = concepts.Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var model in models)
        {
            var name = naming.ToDeclarationName(model.Name);
            if (name.Length <= 1 || taken.Contains(name) || declared.ContainsKey(name))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UndeclarableShape,
                    $"'{model.Name}' is a record an artifact carries, and no type declaration could be written for it because '{name}' is a name the document already uses, so the document names it without saying what is in it",
                    location);
                continue;
            }

            declared[name] = new(name, [.. ToProperties(model)], SourceLocation.Start);
        }

        return [.. declared.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Converts the values a shape carries, keeping one line per name.
    /// </summary>
    /// <param name="model">The shape to convert the values of.</param>
    /// <returns>The properties the body carries.</returns>
    /// <remarks>
    /// Nothing here is reserved - a <c>type</c> body dispatches on nothing, so every name the source declares is
    /// written as it stands. Two values whose names come out the same are the one case a line is dropped, because the
    /// second would be read as declaring the first one twice.
    /// </remarks>
    IEnumerable<PropertySyntax> ToProperties(TypeModel model) =>
        model.Properties
            .Select(_ => new PropertySyntax(naming.ToPropertyName(_.Name), types.Convert(_.Type), SourceLocation.Start))
            .DistinctBy(_ => _.Name, StringComparer.Ordinal);
}
