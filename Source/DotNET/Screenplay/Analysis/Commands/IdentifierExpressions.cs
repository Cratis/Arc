// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Removes only framework-owned identity wrappers and known scalar text conversions.
/// </summary>
internal static class IdentifierExpressions
{
    /// <summary>Reads the property expression behind supported identity wrappers.</summary>
    /// <param name="expression">The provider expression.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>The unwrapped expression, or null for unsupported conversions.</returns>
    internal static ExpressionSyntax? Read(ExpressionSyntax? expression, SemanticModel? model)
    {
        if (expression is null || model is null)
        {
            return null;
        }
        expression = UnwrapSyntax(expression);
        if (!ValueConversions.Preserves(expression, model, PreservesIdentity))
        {
            return null;
        }
        return ReadValue(expression, model);
    }

    /// <summary>Removes parentheses and null suppression syntax.</summary>
    /// <param name="expression">The expression to unwrap.</param>
    /// <returns>The underlying expression.</returns>
    internal static ExpressionSyntax UnwrapSyntax(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parentheses => UnwrapSyntax(parentheses.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppression => UnwrapSyntax(suppression.Operand),
        _ => expression
    };

    /// <summary>Checks whether a formatter preserves a supported scalar identity.</summary>
    /// <param name="method">The resolved formatter.</param>
    /// <param name="invocation">The formatter invocation.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>Whether the formatter is supported.</returns>
    internal static bool ScalarText(IMethodSymbol method, InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (method.Name != "ToString" || method.IsStatic)
        {
            return false;
        }
        var type = method.ContainingType;
        if (type.Is(WellKnownTypeNames.ConceptAs))
        {
            return ConceptText(method, invocation);
        }
        if (type.Is("System.Guid"))
        {
            return UuidText(invocation, model);
        }
        if (type.SpecialType == SpecialType.System_String)
        {
            return invocation.ArgumentList.Arguments.Count == 0;
        }
        return type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64 && IntegralText(invocation, model);
    }

    static ExpressionSyntax? ReadValue(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        CastExpressionSyntax cast => IdentityCast(cast, model) ? Read(cast.Expression, model) : null,
        BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation =>
            IdentityConstructor(creation, model) ? Read(argument.Expression, model) : null,
        InvocationExpressionSyntax invocation => TextValue(invocation, model),
        _ => expression
    };

    static ExpressionSyntax? TextValue(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax member &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol formatter && ScalarText(formatter, invocation, model)
            ? Read(member.Expression, model) : null;

    static bool ConceptText(IMethodSymbol method, InvocationExpressionSyntax invocation) =>
        method.IsSealed && invocation.ArgumentList.Arguments.Count == 0 &&
        method.ContainingType.TypeArguments is [var backing] && Scalar(backing);

    static bool UuidText(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 0 ||
        (invocation.ArgumentList.Arguments is [var format] && model.GetConstantValue(format.Expression) is { HasValue: true, Value: string value } && value == "D");

    static bool IntegralText(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 0 ||
        (invocation.ArgumentList.Arguments is [var provider] &&
            model.GetSymbolInfo(provider.Expression).Symbol is IPropertySymbol { IsStatic: true } culture && culture.Name == "InvariantCulture" &&
            culture.ContainingType.Is("System.Globalization.CultureInfo"));

    static bool IdentityCast(CastExpressionSyntax cast, SemanticModel model) =>
        model.GetOperation(cast) is IConversionOperation conversion &&
        (conversion.Conversion.IsIdentity || (conversion.OperatorMethod is { } method && PreservesIdentity(method)));

    static bool IdentityConstructor(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor &&
        constructor.ContainingType.Is("Cratis.Chronicle.Events.EventSourceId") &&
        constructor.Parameters is [var parameter] &&
        (parameter.Type.SpecialType == SpecialType.System_String || parameter.Type.Is("System.Guid"));

    static bool Scalar(ITypeSymbol type) => type.Is("System.Guid") ||
        type.SpecialType is SpecialType.System_String or SpecialType.System_Int32 or SpecialType.System_Int64;

    static bool PreservesIdentity(IMethodSymbol method) => ConceptValue(method) || EventSourceValue(method);

    static bool ConceptValue(IMethodSymbol method) =>
        method.ContainingType.Is(WellKnownTypeNames.ConceptAs) && method.ContainingType.TypeArguments is [var backing] &&
        Scalar(backing) && SymbolEqualityComparer.Default.Equals(method.ReturnType, backing);

    static bool EventSourceValue(IMethodSymbol method) =>
        (method.ContainingType.Is("Cratis.Chronicle.Events.EventSourceId") ||
            method.ContainingType.Is("Cratis.Chronicle.Events.EventSourceId`1")) &&
        method.ReturnType.Is("Cratis.Chronicle.Events.EventSourceId");
}
