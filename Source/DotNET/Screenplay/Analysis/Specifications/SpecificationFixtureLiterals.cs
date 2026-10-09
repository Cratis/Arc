// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads concrete fixture scalars without treating generated identities or arbitrary constructors as literals.
/// </summary>
/// <param name="models">The models owning held fixture initializers.</param>
/// <param name="heldValues">The stability analysis of held fixtures.</param>
internal class SpecificationFixtureLiterals(SemanticModels? models, HeldValues? heldValues)
{
    /// <summary>
    /// Reads an inline scalar or a single stable initializer of the same declared type.
    /// </summary>
    /// <param name="expression">The fixture expression.</param>
    /// <param name="model">The model owning it.</param>
    /// <param name="expected">The fixture property's type.</param>
    /// <returns>A proven literal, or null when its value is not proven.</returns>
    public LiteralSource? Read(ExpressionSyntax expression, SemanticModel model, ITypeSymbol expected)
    {
        if ((FromConversion(expression, model, expected) ?? Inline(expression, model, expected)) is { } inline)
        {
            return inline;
        }

        expression = MappingSourceReader.Unwrap(expression);
        var symbol = expression is IdentifierNameSyntax or MemberAccessExpressionSyntax ? model.GetSymbolInfo(expression).Symbol : null;
        var held = heldValues ?? new HeldValues(models ?? new SemanticModels([model.Compilation]));
        if (symbol is null || !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(expression).Type, expected))
        {
            return null;
        }

        if (expression is MemberAccessExpressionSyntax access && !symbol.IsStatic &&
            MappingSourceReader.Unwrap(access.Expression) is not ThisExpressionSyntax)
        {
            var receiver = model.GetSymbolInfo(access.Expression).Symbol;
            if (receiver is null || !held.IsStable(receiver, model.Compilation))
            {
                return null;
            }
        }

        if (held.InitializerOf(symbol, model.Compilation) is not { } initializer ||
            (models ?? new SemanticModels([model.Compilation])).For(initializer.SyntaxTree) is not { } initializerModel)
        {
            return null;
        }

        return Inline(initializer, initializerModel, expected);
    }

    static LiteralSource? Inline(ExpressionSyntax expression, SemanticModel model, ITypeSymbol expected)
    {
        expression = MappingSourceReader.Unwrap(expression);
        if (expected.Is("System.Guid"))
        {
            var text = expression switch
            {
                BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null }
                    when model.GetSymbolInfo(expression).Symbol is IMethodSymbol { Parameters: [var parameter] } constructor &&
                         constructor.ContainingType.Is("System.Guid") && parameter.Type.SpecialType == SpecialType.System_String => argument.Expression,
                InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] }
                    when model.GetSymbolInfo(expression).Symbol is IMethodSymbol { IsStatic: true, Parameters: [var parameter] } method &&
                         string.Equals(method.Name, "Parse", StringComparison.Ordinal) && method.ContainingType.Is("System.Guid") &&
                         parameter.Type.SpecialType == SpecialType.System_String => argument.Expression,
                _ => null
            };

            return text is not null && model.GetConstantValue(text) is { HasValue: true, Value: string value } && Guid.TryParse(value, out var guid)
                ? new(guid.ToString("D"))
                : null;
        }

        if (expression is BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [var supplied], Initializer: null } creation &&
            model.GetTypeInfo(creation).Type is INamedTypeSymbol constructed &&
            SymbolEqualityComparer.Default.Equals(constructed, expected) &&
            model.GetSymbolInfo(creation).Symbol is IMethodSymbol { Parameters: [var input] } selected &&
            (constructed.Is(WellKnownTypeNames.EventSourceId) || IsPassThroughConcept(constructed, selected, input)))
        {
            return Inline(supplied.Expression, model, input.Type);
        }

        if (expected.FindBase(WellKnownTypeNames.ConceptAs) is not null && !expected.Is(WellKnownTypeNames.EventSourceId))
        {
            return null;
        }

        return model.GetConstantValue(expression) is { HasValue: true } constant &&
            StatableValues.TryState(expected, constant.Value, out var literal)
            ? new(literal)
            : null;
    }

    static bool IsPassThroughConcept(INamedTypeSymbol type, IMethodSymbol constructor, IParameterSymbol parameter) =>
        type.IsRecord &&
        type.BaseType is { TypeArguments: [var backing] } baseType &&
        baseType.OriginalDefinition.Is(WellKnownTypeNames.ConceptAs) &&
        SymbolEqualityComparer.Default.Equals(backing, parameter.Type) &&
        constructor.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToArray() is
            [RecordDeclarationSyntax { BaseList: { } bases }] &&
        bases.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault() is
            { ArgumentList.Arguments: [var argument] } &&
        argument.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == parameter.Name &&
        type.GetMembers("Value").OfType<IPropertySymbol>().All(property =>
            property.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is ParameterSyntax));

    LiteralSource? FromConversion(ExpressionSyntax expression, SemanticModel model, ITypeSymbol expected)
    {
        var conversion = (model.GetOperation(expression) as IConversionOperation)?.OperatorMethod ?? model.GetConversion(expression).MethodSymbol;
        if (conversion is not { Parameters: [var parameter] } ||
            parameter.Type.SpecialType != SpecialType.System_String ||
            !SymbolEqualityComparer.Default.Equals(conversion.ReturnType, expected) ||
            model.GetConstantValue(MappingSourceReader.Unwrap(expression)) is not { HasValue: true, Value: string text } ||
            conversion.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).ToArray() is not
                [ConversionOperatorDeclarationSyntax { ExpressionBody.Expression: BaseObjectCreationExpressionSyntax
                    { ArgumentList.Arguments: [var argument], Initializer: null } creation }] ||
            (models ?? new SemanticModels([model.Compilation])).For(creation.SyntaxTree) is not { } bodyModel ||
            bodyModel.GetSymbolInfo(creation).Symbol is not IMethodSymbol { Parameters: [var input] } constructor ||
            !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, expected) ||
            !IsPassThroughConcept(constructor.ContainingType, constructor, input))
        {
            return null;
        }

        if (input.Type.SpecialType == SpecialType.System_String &&
            SymbolEqualityComparer.Default.Equals(bodyModel.GetSymbolInfo(argument.Expression).Symbol, parameter))
        {
            return new(text);
        }

        if (input.Type.Is("System.Guid") && argument.Expression is InvocationExpressionSyntax { ArgumentList.Arguments: [var source] } parse &&
            bodyModel.GetSymbolInfo(parse).Symbol is IMethodSymbol { IsStatic: true, Parameters: [var parsedParameter] } method &&
            string.Equals(method.Name, "Parse", StringComparison.Ordinal) && method.ContainingType.Is("System.Guid") &&
            parsedParameter.Type.SpecialType == SpecialType.System_String &&
            SymbolEqualityComparer.Default.Equals(bodyModel.GetSymbolInfo(source.Expression).Symbol, parameter) && Guid.TryParse(text, out var guid))
        {
            return new(guid.ToString("D"));
        }

        return null;
    }
}
