// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Proves a local list is only populated by unconditional additions and returned.
/// </summary>
internal static class ReturnedAccumulator
{
    /// <summary>
    /// Reads the original expressions added to the returned list.
    /// </summary>
    /// <param name="returned">The returned local reference.</param>
    /// <param name="model">The semantic model resolving the list and its uses.</param>
    /// <returns>The additions, or null for any other behavior.</returns>
    internal static IReadOnlyList<ExpressionSyntax>? Read(ExpressionSyntax returned, SemanticModel model)
    {
        if (BodyOf(returned) is not { } block)
        {
            return null;
        }
        if (InitialLocal(block, model) is not { } local)
        {
            return null;
        }
        if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(returned).Symbol, local))
        {
            return null;
        }
        var items = block.Statements.Skip(1).SkipLast(1).Select(statement => Addition(statement, local, model)).ToList();

        return items.Exists(item => item is null) ? null : items.OfType<ExpressionSyntax>().ToList();
    }

    static BlockSyntax? BodyOf(ExpressionSyntax returned)
    {
        if (returned is not IdentifierNameSyntax)
        {
            return null;
        }
        if (returned.Parent is not ReturnStatementSyntax { Parent: BlockSyntax block } statement)
        {
            return null;
        }

        return block.Statements.Last() == statement ? block : null;
    }

    static ILocalSymbol? InitialLocal(BlockSyntax block, SemanticModel model)
    {
        if (block.Statements.FirstOrDefault() is not LocalDeclarationStatementSyntax
            {
                UsingKeyword.RawKind: 0,
                AwaitKeyword.RawKind: 0,
                Declaration.Variables: [var variable]
            })
        {
            return null;
        }
        if (model.GetDeclaredSymbol(variable) is not ILocalSymbol local)
        {
            return null;
        }

        return IsEmptyList(variable, local, model) ? local : null;
    }

    static bool IsEmptyList(VariableDeclaratorSyntax variable, ILocalSymbol local, SemanticModel model)
    {
        if (local.Type is not INamedTypeSymbol type || type.OriginalDefinition.FullMetadataName() != "System.Collections.Generic.List`1")
        {
            return false;
        }

        return variable.Initializer?.Value switch
        {
            CollectionExpressionSyntax { Elements.Count: 0 } => true,
            BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } creation => IsEmptyCreation(creation, local, model),
            _ => false
        };
    }

    static bool IsEmptyCreation(BaseObjectCreationExpressionSyntax creation, ILocalSymbol local, SemanticModel model) =>
        SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(creation).Type, local.Type) &&
        (creation.Initializer is null || creation.Initializer.Expressions.Count == 0);

    static ExpressionSyntax? Addition(StatementSyntax statement, ILocalSymbol local, SemanticModel model)
    {
        if (statement is not ExpressionStatementSyntax
            {
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver, Name.Identifier.ValueText: "Add" },
                    ArgumentList.Arguments: [var item]
                } invocation
            })
        {
            return null;
        }
        if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, local))
        {
            return null;
        }
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingType: { } owner })
        {
            return null;
        }
        if (!SymbolEqualityComparer.Default.Equals(owner, local.Type))
        {
            return null;
        }

        return item.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Any(identifier => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, local)) ? null : item.Expression;
    }
}
