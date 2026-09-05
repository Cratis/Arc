// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads where the value of an expression inside a command handler comes from.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Only two sources survive into a document - a path into the command's own input, and a constant. Anything else is
/// code, and a mapping guessing at code would be worse than a mapping that is not there.
/// <para>
/// An expression read from a body the handler called rather than from the handler itself names that body's
/// parameters, so the bindings of the call site stand in for them and the value is followed back to the command.
/// </para>
/// </remarks>
public class MappingSourceReader(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads the dotted path an expression walks into the command's own input.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The path, or <see langword="null"/> when the expression does not walk into the input.</returns>
    public static string? ReadPath(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ParameterBindings? bindings = null)
    {
        var segments = new List<string>();
        var current = Unwrap(expression);

        while (true)
        {
            switch (current)
            {
                case MemberAccessExpressionSyntax member:
                    segments.Insert(0, member.Name.Identifier.ValueText);
                    current = Unwrap(member.Expression);
                    continue;

                case ThisExpressionSyntax:
                    return segments.Count == 0 ? null : string.Join('.', segments);

                case IdentifierNameSyntax identifier when bindings?.Resolve(identifier, semanticModel) is { } bound:
                    return ReadPath(bound.Expression, bound.SemanticModel, owner) is { } prefix
                        ? string.Join('.', segments.Prepend(prefix))
                        : null;

                case IdentifierNameSyntax identifier:
                    segments.Insert(0, identifier.Identifier.ValueText);

                    return IsOwnInput(identifier, semanticModel, owner) ? string.Join('.', segments) : null;

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Strips the wrappers that do not change what an expression yields.
    /// </summary>
    /// <param name="expression">The expression to strip.</param>
    /// <returns>The wrapped expression.</returns>
    public static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        CastExpressionSyntax cast => Unwrap(cast.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.SuppressNullableWarningExpression } suppress =>
            Unwrap(suppress.Operand),
        _ => expression
    };

    /// <summary>
    /// Reads the source of an expression.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The source, or <see langword="null"/> when the expression is not expressible.</returns>
    public MappingSourceModel? Read(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        string location,
        ParameterBindings? bindings = null)
    {
        var unwrapped = Unwrap(expression);

        var constant = semanticModel.GetConstantValue(unwrapped);
        if (constant.HasValue)
        {
            return new LiteralSource(ConstantOf(expression, semanticModel, constant.Value, location));
        }

        if (bindings?.Resolve(unwrapped, semanticModel) is { } bound)
        {
            return Read(bound.Expression, bound.SemanticModel, owner, location);
        }

        var path = ReadPath(unwrapped, semanticModel, owner, bindings);

        return path is null ? null : new PropertyPathSource(path);
    }

    /// <summary>
    /// Reads the exact literal a Stage-generated query specification authors for one exact declared type.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="expectedType">The exact formal or property type in the specification compilation.</param>
    /// <param name="sourceExpectedType">The corresponding exact type declared by the application.</param>
    /// <returns>The literal, or <see langword="null"/> when the expression is not an exact supported query value.</returns>
    /// <remarks>
    /// This is deliberately additive rather than part of <see cref="Read"/>. Direct concept construction and
    /// framework parse calls are syntax emitted by Stage query specifications, not globally valid mapping sources
    /// for commands, events, or projections. Unlike the legacy literal reader, this reader proves that the authored
    /// value has the exact type Stage rendered it for before returning the value.
    /// </remarks>
    public LiteralSource? ReadQueryLiteral(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol expectedType,
        ITypeSymbol sourceExpectedType)
    {
        if (!IsExactScalarType(expectedType) ||
            !IsExactScalarType(sourceExpectedType) ||
            !TypesHaveSameIdentity(expectedType, sourceExpectedType))
        {
            return null;
        }

        var unwrapped = UnwrapQueryValue(expression);
        if (expectedType.TypeKind == TypeKind.Enum)
        {
            return EnumValueOf(unwrapped, semanticModel, expectedType, sourceExpectedType) is { } enumeration
                ? new LiteralSource(enumeration)
                : null;
        }

        if (expectedType is not INamedTypeSymbol namedExpected || sourceExpectedType is not INamedTypeSymbol namedSourceExpected)
        {
            return null;
        }

        if (namedExpected.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var backing] } &&
            namedSourceExpected.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var sourceBacking] })
        {
            return ConceptValueOf(unwrapped, semanticModel, namedExpected, namedSourceExpected, backing, sourceBacking) is { } conceptValue
                ? ReadQueryLiteral(conceptValue, semanticModel, backing, sourceBacking)
                : null;
        }

        if (ParsedTextOf(unwrapped, semanticModel, namedExpected) is { } parsed)
        {
            return new LiteralSource(parsed);
        }

        return PrimitiveValueOf(unwrapped, semanticModel, namedExpected) is { } primitive
            ? new LiteralSource(primitive)
            : null;
    }

    /// <summary>
    /// Gets the single exact backing value a strongly typed concept is constructed from.
    /// </summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="expectedType">The exact concept type in the specification compilation.</param>
    /// <param name="sourceExpectedType">The corresponding application-declared concept type.</param>
    /// <param name="backing">The exact backing type in the specification compilation.</param>
    /// <param name="sourceBacking">The corresponding application-declared backing type.</param>
    /// <returns>The exact backing expression, or <see langword="null"/> when the expression is not a direct Stage concept construction.</returns>
    static ExpressionSyntax? ConceptValueOf(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        INamedTypeSymbol expectedType,
        INamedTypeSymbol sourceExpectedType,
        ITypeSymbol backing,
        ITypeSymbol sourceBacking)
    {
        if (expression is not ObjectCreationExpressionSyntax
            {
                ArgumentList.Arguments: [var argument],
                Initializer: null
            } creation ||
            argument.NameColon is not null ||
            semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol constructed ||
            !SymbolEqualityComparer.IncludeNullability.Equals(constructed, expectedType) ||
            semanticModel.GetSymbolInfo(creation).Symbol is not IMethodSymbol { Parameters: [var selectedParameter] } selectedConstructor ||
            !SymbolEqualityComparer.IncludeNullability.Equals(selectedParameter.Type, backing) ||
            !IsExactStageConcept(sourceExpectedType, sourceBacking, out var primaryConstructor) ||
            !ConstructorMatches(selectedConstructor, primaryConstructor))
        {
            return null;
        }

        return argument.Expression;
    }

    /// <summary>
    /// Reads one exact enumeration member.
    /// </summary>
    /// <param name="expression">The authored member expression.</param>
    /// <param name="semanticModel">The semantic model owning it.</param>
    /// <param name="expectedType">The exact enumeration in the specification compilation.</param>
    /// <param name="sourceExpectedType">The corresponding application enumeration.</param>
    /// <returns>The named enumeration member, or <see langword="null"/> for casts and undeclared values.</returns>
    static EnumValue? EnumValueOf(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol expectedType,
        ITypeSymbol sourceExpectedType)
    {
        if (semanticModel.GetSymbolInfo(expression).Symbol is not IFieldSymbol
            {
                HasConstantValue: true,
                ContainingType: { } containingType
            } member ||
            !SymbolEqualityComparer.IncludeNullability.Equals(containingType, expectedType) ||
            !sourceExpectedType.GetMembers(member.Name).OfType<IFieldSymbol>().Any(_ => _.HasConstantValue))
        {
            return null;
        }

        return new(member.Name);
    }

    /// <summary>
    /// Reads one primitive literal with the exact runtime semantics Stage emits for its declared primitive.
    /// </summary>
    /// <param name="expression">The authored primitive expression.</param>
    /// <param name="semanticModel">The semantic model owning it.</param>
    /// <param name="expectedType">The exact expected primitive type.</param>
    /// <returns>The primitive value, or <see langword="null"/> when its syntax or runtime type does not match.</returns>
    static object? PrimitiveValueOf(ExpressionSyntax expression, SemanticModel semanticModel, INamedTypeSymbol expectedType)
    {
        if (!IsPrimitiveLiteral(expression) ||
            !SymbolEqualityComparer.IncludeNullability.Equals(semanticModel.GetTypeInfo(expression).Type, expectedType) ||
            semanticModel.GetConstantValue(expression) is not { HasValue: true } constant ||
            constant.Value is null)
        {
            return null;
        }

        return expectedType.FullMetadataName() switch
        {
            "System.String" when constant.Value is string => constant.Value,
            "System.Int32" when constant.Value is int => constant.Value,
            "System.Decimal" when constant.Value is decimal => constant.Value,
            "System.Boolean" when constant.Value is bool => constant.Value,
            _ => null
        };
    }

    static bool IsPrimitiveLiteral(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax)
        {
            return true;
        }

        return expression is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } prefix &&
            (prefix.RawKind == (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryMinusExpression ||
             prefix.RawKind == (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryPlusExpression);
    }

    /// <summary>
    /// Reads the canonical text from the exact parse calls Stage emits for non-constant scalar values.
    /// </summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="semanticModel">The semantic model owning the expression.</param>
    /// <param name="expectedType">The exact primitive type the parsed text must produce.</param>
    /// <returns>The canonical parsed text, or <see langword="null"/> for every other call shape.</returns>
    static string? ParsedTextOf(ExpressionSyntax expression, SemanticModel semanticModel, INamedTypeSymbol expectedType)
    {
        if (expression is not InvocationExpressionSyntax invocation ||
            DotNetInvocations.MethodFor(invocation, semanticModel) is not { } method ||
            invocation.ArgumentList.Arguments is not [var textArgument, ..] ||
            textArgument.Expression is not LiteralExpressionSyntax ||
            semanticModel.GetConstantValue(textArgument.Expression) is not { HasValue: true, Value: string text })
        {
            return null;
        }

        var definition = DotNetInvocations.DefinitionOf(method);
        if (definition is not { IsStatic: true, Name: "Parse", TypeParameters.Length: 0 } ||
            definition.Parameters.FirstOrDefault()?.Type.SpecialType != SpecialType.System_String ||
            !SymbolEqualityComparer.IncludeNullability.Equals(method.ReturnType, expectedType))
        {
            return null;
        }

        var containingType = definition.ContainingType.FullMetadataName();
        if (containingType == "System.Guid" &&
            expectedType.FullMetadataName() == containingType &&
            definition.Parameters.Length == 1 &&
            invocation.ArgumentList.Arguments.Count == 1 &&
            Guid.TryParse(text, out _))
        {
            return text;
        }

        if (containingType is not ("System.DateOnly" or "System.DateTimeOffset") ||
            expectedType.FullMetadataName() != containingType ||
            definition.Parameters.Length != 2 ||
            invocation.ArgumentList.Arguments is not [_, var cultureArgument] ||
            semanticModel.GetSymbolInfo(UnwrapQueryValue(cultureArgument.Expression)).Symbol is not IPropertySymbol
            {
                IsStatic: true,
                Name: "InvariantCulture"
            } culture ||
            culture.ContainingType.FullMetadataName() != "System.Globalization.CultureInfo")
        {
            return null;
        }

        var valid = containingType == "System.DateOnly"
            ? DateOnly.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)
            : DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
        return valid ? text : null;
    }

    static bool IsExactScalarType(ITypeSymbol type) =>
        type.NullableAnnotation != NullableAnnotation.Annotated &&
        type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        CollectionElements.ElementOf(type) is null;

    static bool IsExactStageConcept(INamedTypeSymbol type, ITypeSymbol backing, out IMethodSymbol primaryConstructor)
    {
        primaryConstructor = null!;
        if (!type.IsRecord ||
            type.BaseType is not { TypeArguments: [var directBacking] } directBase ||
            directBase.OriginalDefinition.FullMetadataName() is not (WellKnownTypeNames.ConceptAs or WellKnownTypeNames.EventSourceIdOfT) ||
            !SymbolEqualityComparer.IncludeNullability.Equals(directBacking, backing) ||
            type.InstanceConstructors.SingleOrDefault(IsPrimaryRecordConstructor) is not { Parameters: [var parameter] } found ||
            !SymbolEqualityComparer.IncludeNullability.Equals(parameter.Type, backing) ||
            found.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<RecordDeclarationSyntax>().SingleOrDefault() is not { } declaration ||
            declaration.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault() is not
            {
                ArgumentList.Arguments: [var baseArgument]
            } ||
            baseArgument.Expression is not IdentifierNameSyntax identifier ||
            !string.Equals(identifier.Identifier.ValueText, parameter.Name, StringComparison.Ordinal))
        {
            return false;
        }

        primaryConstructor = found;
        return true;
    }

    static bool IsPrimaryRecordConstructor(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Any(_ => _.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null });

    static bool ConstructorMatches(IMethodSymbol selected, IMethodSymbol primary) =>
        selected.Parameters.Length == primary.Parameters.Length &&
        selected.Parameters.Zip(primary.Parameters).All(pair =>
            string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
            TypesHaveSameIdentity(pair.First.Type, pair.Second.Type));

    static bool TypesHaveSameIdentity(ITypeSymbol first, ITypeSymbol second) =>
        first.NullableAnnotation == second.NullableAnnotation &&
        string.Equals(
            first.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            second.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            StringComparison.Ordinal);

    static ExpressionSyntax UnwrapQueryValue(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => UnwrapQueryValue(parenthesized.Expression),
        _ => expression
    };

    /// <summary>
    /// Determines whether an identifier names a property of the command itself.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the identifier lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <returns>True when the identifier resolves to the command's own input.</returns>
    static bool IsOwnInput(IdentifierNameSyntax identifier, SemanticModel semanticModel, ITypeSymbol owner) =>
        semanticModel.GetSymbolInfo(identifier).Symbol is IPropertySymbol property &&
        SymbolEqualityComparer.Default.Equals(property.ContainingType, owner);

    /// <summary>
    /// Recovers what a constant stands for, naming the member when it belongs to an enumeration.
    /// </summary>
    /// <param name="expression">The expression the constant was read from.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="value">The value the compiler handed over.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The value to carry, which for an enumeration is the member it names.</returns>
    object? ConstantOf(ExpressionSyntax expression, SemanticModel semanticModel, object? value, string location)
    {
        if (EnumConstants.EnumerationOf(expression, semanticModel) is not { } enumeration)
        {
            return value;
        }

        if (EnumConstants.TryResolve(enumeration, value, out var member))
        {
            return member;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnnamedEnumerationValue,
            $"'{enumeration.Name}' declares no member with the value '{value}', so it is written as that number rather than as a name the concept declares",
            location);

        return value;
    }
}
