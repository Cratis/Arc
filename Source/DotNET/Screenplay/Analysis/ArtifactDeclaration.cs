// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Holds where the declaration that artifacts were recovered from is written.
/// </summary>
/// <remarks>
/// A type is written in as many places as it has partial declarations, and a source generator contributing one of
/// them is exactly the case this exists to tell apart, so every place it is written is held rather than the first.
/// </remarks>
public class ArtifactDeclaration
{
    readonly (SyntaxTree Tree, TextSpan Span)[] _written;

    ArtifactDeclaration(int artifacts, (SyntaxTree Tree, TextSpan Span)[] written)
    {
        Artifacts = artifacts;
        _written = written;
    }

    /// <summary>
    /// Gets the number of artifacts recovered from the declaration.
    /// </summary>
    public int Artifacts { get; }

    /// <summary>
    /// Holds where the type artifacts were recovered from is written.
    /// </summary>
    /// <param name="type">The type they were recovered from.</param>
    /// <param name="artifacts">The number of artifacts recovered from it.</param>
    /// <returns>The <see cref="ArtifactDeclaration"/>.</returns>
    public static ArtifactDeclaration For(INamedTypeSymbol type, int artifacts) =>
        new(artifacts, [.. type.DeclaringSyntaxReferences.Select(_ => (_.SyntaxTree, _.Span))]);

    /// <summary>
    /// Answers whether something the compiler reported sits within the declaration.
    /// </summary>
    /// <param name="diagnostic">The diagnostic the compiler reported.</param>
    /// <returns>True when it sits within the declaration.</returns>
    /// <remarks>
    /// An error somewhere else in the compilation says nothing about what was read here, while one written inside
    /// the declaration itself is the compiler saying it could not make sense of the very source the artifact was
    /// recovered from. Telling the two apart is the whole reason the spans are kept.
    /// </remarks>
    public bool Encloses(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree is { } tree &&
        _written.Any(_ => ReferenceEquals(_.Tree, tree) && _.Span.Contains(diagnostic.Location.SourceSpan));
}
