// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads routing scalars through framework-owned, value-preserving conversions only.
/// </summary>
internal static class ProductionValues
{
    /// <summary>
    /// Reads a required command input or a registered generated value without erasing its nominal type.
    /// </summary>
    /// <param name="expression">The supplied routing value.</param>
    /// <param name="model">The semantic model.</param>
    /// <param name="command">The input owner.</param>
    /// <param name="sources">The generated value origins.</param>
    /// <returns>The direct input path, or null.</returns>
    internal static string? Path(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol command, AuthoringSources? sources)
    {
        if (!ValueConversions.Preserves(expression, model, method => FrameworkScalar(method.ContainingType)))
        {
            return null;
        }
        if (expression is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax &&
            sources?.ReadGeneratedPath(expression, model) is { } generatedExpression && !generatedExpression.Contains('.', StringComparison.Ordinal))
        {
            return generatedExpression;
        }
        var value = Unwrap(expression, model);
        if (value is null)
        {
            return null;
        }
        if (sources?.ReadGeneratedPath(value, model) is { } generated && !generated.Contains('.', StringComparison.Ordinal))
        {
            return generated;
        }
        var path = MappingSourceReader.ReadPath(value, model, command);
        var property = command.DeclaredProperties().SingleOrDefault(property => property.Name == path);

        var handler = expression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

        return property is not null && Portable(property.Type) && handler is not null && CommandAuthoringReader.IsUnchanged(property, handler, model)
            ? property.Name : null;
    }

    /// <summary>
    /// Reads exact scalar text without interpreting wrapper text as a runtime template.
    /// </summary>
    /// <param name="expression">The wrapper value.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>The constant text, or null.</returns>
    internal static string? Text(ExpressionSyntax expression, SemanticModel model) =>
        Unwrap(expression, model) is { } value && model.GetConstantValue(value) is { HasValue: true, Value: string text } ? text : null;

    /// <summary>
    /// Checks the portable scalar subset used for source identifiers and stream keys.
    /// </summary>
    /// <param name="type">The nominal input type.</param>
    /// <returns>Whether it is a required portable scalar.</returns>
    internal static bool Portable(ITypeSymbol type)
    {
        var backing = type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments.FirstOrDefault();
        var scalar = backing ?? type;

        return type.NullableAnnotation != NullableAnnotation.Annotated &&
            (scalar.SpecialType == SpecialType.System_String || scalar.Is("System.Guid") ||
                scalar.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64);
    }

    static ExpressionSyntax? Unwrap(ExpressionSyntax expression, SemanticModel model)
    {
        expression = IdentifierExpressions.UnwrapSyntax(expression);
        if (!ValueConversions.Preserves(expression, model, method => FrameworkScalar(method.ContainingType)))
        {
            return null;
        }
        if (expression is CastExpressionSyntax cast)
        {
            return model.GetOperation(cast) is IConversionOperation conversion &&
                (conversion.Conversion.IsIdentity || (conversion.OperatorMethod is { } method && FrameworkScalar(method.ContainingType)))
                ? Unwrap(cast.Expression, model) : null;
        }
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation &&
            model.GetSymbolInfo(invocation).Symbol is IMethodSymbol formatter && IdentifierExpressions.ScalarText(formatter, invocation, model))
        {
            return Unwrap(member.Expression, model);
        }
        if (expression is BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation)
        {
            return model.GetTypeInfo(creation).Type is { } type && FrameworkScalar(type) ? Unwrap(argument.Expression, model) : null;
        }

        return expression;
    }

    static bool FrameworkScalar(ITypeSymbol type) => type.Is("Cratis.Chronicle.Events.EventSourceId") ||
        type.Is("Cratis.Chronicle.Events.EventSourceId`1") || type.Is("Cratis.Chronicle.Events.EventSourceType") ||
        type.Is("Cratis.Chronicle.Events.EventStreamType") || type.Is("Cratis.Chronicle.Events.EventStreamId") ||
        type.Is(WellKnownTypeNames.ConceptAs);
}
