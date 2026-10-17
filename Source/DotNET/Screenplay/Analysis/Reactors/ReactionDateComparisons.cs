// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reports chronological reaction guards that must remain file-backed under decision 0087.
/// </summary>
internal static class ReactionDateComparisons
{
    /// <summary>
    /// Reports date comparisons without claiming a declarative reaction condition.
    /// </summary>
    /// <param name="reactor">The reactor.</param>
    /// <param name="handlers">Its handlers.</param>
    /// <param name="models">The source models.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    internal static void Report(INamedTypeSymbol reactor, IReadOnlyList<IMethodSymbol> handlers, SemanticModels models, ScreenplayDiagnostics? diagnostics)
    {
        foreach (var body in handlers.SelectMany(HandlerBodies.Of))
        {
            if (models.For(body.SyntaxTree) is not { } model)
            {
                continue;
            }
            foreach (var comparison in body.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Where(binary =>
                ComparisonKinds.Of(binary.Kind()) is not null && DateConditions.Applies(binary, model)))
            {
                diagnostics?.Warning(ScreenplayDiagnosticCodes.UnmappableCommandProduction, $"The reaction date comparison '{comparison}' in '{reactor.Name}' was left in code; date ordering in reaction where conditions is not admitted by Screenplay", reactor.ToDisplayString());
            }
        }
    }
}
