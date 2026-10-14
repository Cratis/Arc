// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads refusal values only where Arc and the portable String contract agree.
/// </summary>
internal static class RefusalMappings
{
    /// <summary>
    /// Reads a direct refusal property, including a nullability assertion on ConstraintName.
    /// </summary>
    /// <param name="expression">The mapped expression.</param>
    /// <param name="model">The model resolving its property and receiver.</param>
    /// <param name="target">The target property type.</param>
    /// <param name="refusal">The branch's classified refusal.</param>
    /// <param name="constraint">Whether the branch selects constraint refusals.</param>
    /// <returns>The portable member, or null when its semantics or type differ.</returns>
    internal static RefusalSource? Read(ExpressionSyntax expression, SemanticModel model, ITypeSymbol target, IParameterSymbol refusal, bool constraint)
    {
        if (expression is PostfixUnaryExpressionSyntax suppression && suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
        {
            expression = suppression.Operand;
        }
        if (target.SpecialType != SpecialType.System_String ||
            expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member ||
            model.GetSymbolInfo(member).Symbol is not IPropertySymbol { IsStatic: false } property ||
            !property.ContainingType.Is("Cratis.Arc.Commands.CommandRefusal") ||
            !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, refusal))
        {
            return null;
        }

        // Kind is an enum; its names and ToString values are not the lowercase String reason contract.
        return property.Name switch
        {
            "Message" => new("message"),
            "ConstraintName" when constraint => new("constraint"),
            _ => null
        };
    }
}
