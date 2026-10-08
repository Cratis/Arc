// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Finds explicit constructor exits that can prevent a validation chain from being registered.
/// </summary>
internal static class ValidationExits
{
    /// <summary>
    /// Finds an exit preceding a rule, excluding throws demonstrably caught before registration.
    /// </summary>
    /// <param name="root">The rule registration.</param>
    /// <param name="semanticModel">The model resolving exception and catch types.</param>
    /// <returns>The possible exit, or <see langword="null"/> when none was found.</returns>
    internal static SyntaxNode? Preceding(SyntaxNode root, SemanticModel semanticModel)
    {
        for (var node = root; node is not (null or BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax); node = node.Parent)
        {
            if (node.Parent is BlockSyntax block && block.Statements.TakeWhile(statement => statement != node)
                .SelectMany(ExitsIn)
                .FirstOrDefault(exit => !IsCaughtBefore(exit, root, semanticModel)) is { } exit)
            {
                return exit;
            }
        }

        return null;
    }

    static IEnumerable<SyntaxNode> ExitsIn(SyntaxNode node) =>
        node.DescendantNodesAndSelf(descendIntoChildren: child => child is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
            .Where(child => child is ReturnStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax or GotoStatementSyntax);

    static bool IsCaughtBefore(SyntaxNode exit, SyntaxNode root, SemanticModel semanticModel)
    {
        var expression = exit switch
        {
            ThrowStatementSyntax statement => statement.Expression,
            ThrowExpressionSyntax thrown => thrown.Expression,
            _ => null
        };
        if (exit is not (ThrowStatementSyntax or ThrowExpressionSyntax))
        {
            return false;
        }

        var exceptionType = expression is null ? null : semanticModel.GetTypeInfo(expression).Type;
        var exactType = exceptionType is not null && expression is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax;

        return exit.Ancestors().OfType<TryStatementSyntax>().Any(statement =>
            statement.Block.Span.Contains(exit.Span) && !statement.Span.Contains(root.Span) &&
            (statement.Finally is null || Continues(statement.Finally.Block, semanticModel)) &&
            CatchesAndContinues(statement, exceptionType, exactType, semanticModel));
    }

    static bool CatchesAndContinues(TryStatementSyntax statement, ITypeSymbol? exceptionType, bool exactType, SemanticModel semanticModel)
    {
        foreach (var clause in statement.Catches)
        {
            if (clause.Filter is { } filter && semanticModel.GetConstantValue(filter.FilterExpression) is { HasValue: true, Value: false })
            {
                continue;
            }

            if (!Covers(clause, exceptionType, semanticModel))
            {
                if (!exactType && !Continues(clause.Block, semanticModel))
                {
                    return false;
                }

                continue;
            }

            if (!Continues(clause.Block, semanticModel))
            {
                return false;
            }

            if (clause.Filter is null || semanticModel.GetConstantValue(clause.Filter.FilterExpression) is { HasValue: true, Value: true })
            {
                return true;
            }
        }

        return false;
    }

    static bool Continues(BlockSyntax block, SemanticModel semanticModel) =>
        !ExitsIn(block).Any() && semanticModel.AnalyzeControlFlow(block) is { Succeeded: true, EndPointIsReachable: true };

    static bool Covers(CatchClauseSyntax clause, ITypeSymbol? exceptionType, SemanticModel semanticModel)
    {
        if (clause.Declaration is null)
        {
            return true;
        }

        var caughtType = semanticModel.GetTypeInfo(clause.Declaration.Type).Type;
        if (caughtType is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(caughtType, semanticModel.Compilation.GetTypeByMetadataName("System.Exception")))
        {
            return true;
        }

        for (var type = exceptionType; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, caughtType))
            {
                return true;
            }
        }

        return false;
    }
}
