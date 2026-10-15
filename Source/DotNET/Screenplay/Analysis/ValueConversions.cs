// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Checks semantic conversions that syntax alone does not expose.
/// </summary>
internal static class ValueConversions
{
    /// <summary>
    /// Refuses user-defined conversions unless the caller proves the selected framework operator's semantics.
    /// </summary>
    /// <param name="expression">The value being returned or mapped.</param>
    /// <param name="model">The model binding implicit and explicit operators.</param>
    /// <param name="allows">The operators supported at this boundary.</param>
    /// <returns>Whether every surrounding conversion preserves the recovered meaning.</returns>
    internal static bool Preserves(ExpressionSyntax expression, SemanticModel model, Func<IMethodSymbol, bool> allows)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }
        var operation = model.GetOperation(expression);
        if (operation is null or IInvalidOperation)
        {
            return false;
        }
        while (true)
        {
            if (operation is IConversionOperation { OperatorMethod: { } method } && !allows(method))
            {
                return false;
            }
            if (operation.Parent is not IConversionOperation conversion)
            {
                return true;
            }
            operation = conversion;
        }
    }
}
