// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>Reads exactly one fixture-owned readonly field initialized directly to its expected enum member.</summary>
internal static class RenderedEnumInitializer
{
    /// <summary>Finds an enum member without evaluating a field, getter, alias, or computation.</summary>
    /// <param name="expression">The original field use.</param>
    /// <param name="expected">The expected enum type.</param>
    /// <param name="fixture">The descriptor-owned fixture.</param>
    /// <param name="model">The use's semantic model.</param>
    /// <returns>The direct enum member initializer, or null.</returns>
    internal static ExpressionSyntax? Read(ExpressionSyntax expression, ITypeSymbol expected, INamedTypeSymbol fixture, SemanticModel model)
    {
        if (expected.TypeKind != TypeKind.Enum || expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax) ||
            model.GetSymbolInfo(expression).Symbol is not IFieldSymbol { IsStatic: true, IsReadOnly: true, IsConst: false } field ||
            !SymbolEqualityComparer.Default.Equals(field.Type, expected) ||
            RenderedFieldInitializer.Read(field, fixture, model.Compilation) is not MemberAccessExpressionSyntax initializer ||
            initializer.SyntaxTree != expression.SyntaxTree ||
            model.GetSymbolInfo(initializer).Symbol is not IFieldSymbol { HasConstantValue: true } member ||
            !SymbolEqualityComparer.Default.Equals(member.ContainingType, expected) ||
            !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(initializer.Expression).Symbol, expected))
        {
            return null;
        }

        return initializer;
    }
}
