// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Resolves only Arc's fixed selectors, never similarly spelled user methods or mutable values.
/// </summary>
internal static class RefusalSelectors
{
    const string SelectorType = "Cratis.Arc.Chronicle.Reactors.Refused";

    /// <summary>
    /// Reads a fixed selector and its optional constant constraint identity.
    /// </summary>
    /// <param name="expression">The selector expression.</param>
    /// <param name="model">The model resolving the Arc symbols.</param>
    /// <returns>The selector, or null for an opaque or unspellable selection.</returns>
    internal static (string Selector, string? Constraint)? Read(ExpressionSyntax expression, SemanticModel model)
    {
        var type = model.Compilation.GetTypeByMetadataName(SelectorType);
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (type is null || symbol is null || !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, type))
        {
            return null;
        }
        if (symbol is IPropertySymbol { IsStatic: true } property)
        {
            return property.Name switch
            {
                "ByValidation" => ("validation", null),
                "ByAuthorization" => ("authorization", null),
                "ByAnyConstraint" => ("constraint", null),
                "Any" => ("any", null),
                _ => null
            };
        }

        return symbol is IMethodSymbol { IsStatic: true } && string.Equals(symbol.Name, "ByConstraint", StringComparison.Ordinal) &&
            model.GetOperation(expression) is IInvocationOperation { Arguments: [var argument] } &&
            argument.Value.ConstantValue is { HasValue: true, Value: string name } &&
            ScreenplayIdentifier.IsBareIdentifier(name) && !name.Any(char.IsWhiteSpace)
                ? ("constraint", name) : null;
    }
}
