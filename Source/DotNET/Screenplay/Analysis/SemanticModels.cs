// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Resolves the semantic model of a syntax tree, whichever project of the application it was written in.
/// </summary>
/// <param name="compilations">The compilations being analyzed, ordered.</param>
/// <remarks>
/// A compilation answers only for the trees it was built from and throws for any other, so reading a declaration
/// through the wrong one is not a wrong answer but a crash. Nearly every declaration is read through the compilation
/// that catalogued it, and for those this is that compilation. The exception is a body reached from another body: a
/// command handing its work to an aggregate root written in the project below it reads a behavior whose source
/// belongs to that project, which is exactly the shape a layered application takes.
/// <para>
/// A tree belonging to none of them is a project the caller did not hand over. Nothing is returned rather than
/// something being guessed, and the caller says what was lost.
/// </para>
/// </remarks>
public class SemanticModels(IReadOnlyList<Compilation> compilations)
{
    /// <summary>
    /// Resolves the semantic model a syntax tree is read through.
    /// </summary>
    /// <param name="tree">The tree to read.</param>
    /// <returns>The <see cref="SemanticModel"/>, or <see langword="null"/> when no project holds the tree.</returns>
    public SemanticModel? For(SyntaxTree tree)
    {
        foreach (var compilation in compilations)
        {
            if (compilation.ContainsSyntaxTree(tree))
            {
                return compilation.GetSemanticModel(tree);
            }
        }

        return null;
    }
}
