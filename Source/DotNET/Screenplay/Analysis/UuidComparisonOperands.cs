// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the Uuid operands (decision 0090) that ESM v28 admits in command conditions.
/// </summary>
internal static class UuidComparisonOperands
{
    /// <summary>
    /// Determines whether a type is a Guid, or a concept or identity backed by one.
    /// </summary>
    /// <param name="type">The operand type.</param>
    /// <returns>Whether the type is a Uuid operand type.</returns>
    internal static bool IsUuid(ITypeSymbol? type) => Unwrapped(type) is { } unwrapped && (unwrapped.Is("System.Guid") || Concept(unwrapped) is not null);

    /// <summary>
    /// Gets the concept or identity a Uuid operand is typed as.
    /// </summary>
    /// <param name="type">The operand type.</param>
    /// <returns>The concept type, or null for a bare Guid and for every other type.</returns>
    internal static ITypeSymbol? Concept(ITypeSymbol? type) => Unwrapped(type) is { } unwrapped && !unwrapped.Is("System.Guid") && IsGuidBacked(unwrapped) ? unwrapped : null;

    /// <summary>
    /// Determines whether an operand may be absent.
    /// </summary>
    /// <param name="type">The operand type.</param>
    /// <returns>Whether the type is nullable.</returns>
    internal static bool IsOptional(ITypeSymbol? type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } || type?.NullableAnnotation == NullableAnnotation.Annotated;

    /// <summary>
    /// Reads a Guid, or a concept wrapping one, from source text that is spelled in a way the model can prove.
    /// </summary>
    /// <param name="expression">The operand expression.</param>
    /// <param name="model">Its semantic model.</param>
    /// <returns>The canonical lowercase hyphenated text, or null.</returns>
    internal static string? Literal(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null } creation when IsUuid(model.GetTypeInfo(creation).Type) =>
            model.GetTypeInfo(creation).Type.Is("System.Guid") ? Canonical(model.GetConstantValue(argument.Expression).Value as string) : Literal(Unwrap(argument.Expression), model),
        InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } call when IsGuidParse(call, model) => Canonical(model.GetConstantValue(argument.Expression).Value as string),
        MemberAccessExpressionSyntax access when model.GetSymbolInfo(access).Symbol is IFieldSymbol { IsStatic: true } field && string.Equals(field.Name, "Empty", StringComparison.Ordinal) && field.ContainingType.Is("System.Guid") => Guid.Empty.ToString("D"),
        _ => null
    };

    /// <summary>
    /// Explains why an operand is not an admitted Uuid literal.
    /// </summary>
    /// <param name="expression">The operand expression.</param>
    /// <returns>The refusal reason.</returns>
    internal static string Refusal(ExpressionSyntax expression) =>
        expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(call => call.Expression.ToString().Contains("New", StringComparison.Ordinal) || call.Expression.ToString().Contains("Generate", StringComparison.Ordinal))
            ? "generated values such as Guid.NewGuid() are not admitted"
            : "the operand is neither a canonical Uuid literal nor a command property or read path";

    static ITypeSymbol? Unwrapped(ITypeSymbol? type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var underlying] } ? underlying : type;

    static bool IsGuidBacked(ITypeSymbol type) =>
        (type.FindBase(WellKnownTypeNames.ConceptAs) ?? type.FindBase(WellKnownTypeNames.EventSourceIdOfT)) is { TypeArguments: [var primitive] } && primitive.Is("System.Guid");

    static bool IsGuidParse(InvocationExpressionSyntax call, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsStatic: true, Parameters: [{ Type.SpecialType: SpecialType.System_String }] } method && string.Equals(method.Name, "Parse", StringComparison.Ordinal) && method.ContainingType.Is("System.Guid");

    static ExpressionSyntax Unwrap(ExpressionSyntax expression) => Commands.MappingSourceReader.Unwrap(expression);

    static string? Canonical(string? text) => text is not null && Guid.TryParse(text, out var guid) ? guid.ToString("D") : null;
}
