// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Counts what analysis recovered from a compilation, and remembers where each of it was written.
/// </summary>
/// <remarks>
/// "Nothing recovered from it describes the application reliably" is a claim about a compilation that did not
/// compile, and for most of them it is false - a build handed over without the compile items it generates leaves
/// hundreds of errors behind while every artifact is read exactly as written. This is what turns that claim into
/// one that can be checked: how much was recovered, and how much of it came from source the compiler accepted.
/// </remarks>
public class RecoveredArtifacts
{
    readonly List<ArtifactDeclaration> _declarations = [];

    /// <summary>
    /// Gets the number of artifacts recovered in all.
    /// </summary>
    public int Count => _declarations.Sum(_ => _.Artifacts);

    /// <summary>
    /// Holds what one type contributed, ignoring a type that contributed nothing.
    /// </summary>
    /// <param name="type">The type that was read.</param>
    /// <param name="artifacts">The number of artifacts recovered from it.</param>
    public void Declare(INamedTypeSymbol type, int artifacts)
    {
        if (artifacts <= 0)
        {
            return;
        }

        _declarations.Add(ArtifactDeclaration.For(type, artifacts));
    }

    /// <summary>
    /// Counts the artifacts recovered from a declaration that none of the compilation errors sit inside.
    /// </summary>
    /// <param name="errors">The errors the compiler reported.</param>
    /// <returns>The number of artifacts.</returns>
    /// <remarks>
    /// This is the number that decides how serious a compilation that did not compile is. An artifact read from a
    /// declaration the compiler accepted is described exactly as the source states it, whatever went wrong
    /// elsewhere, so as long as there is one of them the document is not worthless.
    /// </remarks>
    public int RecoveredFromAcceptedSource(IReadOnlyList<Diagnostic> errors) =>
        _declarations.Where(declaration => !errors.Any(declaration.Encloses)).Sum(_ => _.Artifacts);
}
