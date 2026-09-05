// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Reports every type the document had to refer to by a name that does not say what it is.
/// </summary>
/// <remarks>
/// A Screenplay type reference is a single identifier, so a type the grammar cannot hold is written as whatever of its
/// name survives - and the property then claims a type nothing declares. Saying which types those were is the
/// difference between a document with a known gap and a document that is quietly wrong.
/// </remarks>
public static class TypesTheDocumentCannotName
{
    /// <summary>
    /// Reports everything the registry collected that the document could not say properly.
    /// </summary>
    /// <param name="types">The registry holding the types encountered.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="location">Where to report against.</param>
    /// <remarks>
    /// These are reported against the application rather than against the project a type was reached from. One
    /// registry holds the concepts of every project, because a concept is declared once and referred to by name from
    /// there on, so which project first reached a type is an accident of the order the projects are read in and would
    /// be a misleading thing to point at.
    /// </remarks>
    public static void Report(TypeRegistry types, ScreenplayDiagnostics diagnostics, string? location)
    {
        foreach (var type in types.Unmappable)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableTypeReference,
                $"'{type}' has no Screenplay counterpart, so it is referred to by a name the document never declares",
                location);
        }

        foreach (var type in types.Ambiguous)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AmbiguousConceptName,
                $"'{type}' shares its simple name with a concept the document already declares, so what it is is described by the first one instead",
                location);
        }

        foreach (var shape in types.Shapes)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UndeclarableShape,
                $"'{shape.Type}' is a record an artifact carries, and no type declaration could be written for it because {shape.Reason}, so the document names it without saying what is in it",
                location);
        }
    }
}
