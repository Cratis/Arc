// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the required scalar property supplying a command's event source identity.
/// </summary>
/// <param name="models">The semantic models used to resolve a self-provided identity.</param>
/// <param name="diagnostics">Where ambiguous or unreadable identities are reported.</param>
public class CommandIdentifierReader(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    const string EventSourceId = "Cratis.Chronicle.Events.EventSourceId";
    const string GenericEventSourceId = "Cratis.Chronicle.Events.EventSourceId`1";
    const string Provider = "Cratis.Chronicle.Events.ICanProvideEventSourceId";
    const string Key = "Cratis.Chronicle.Keys.KeyAttribute";

    /// <summary>
    /// Reads the identifier without choosing arbitrarily between candidates.
    /// </summary>
    /// <param name="command">The command to read.</param>
    /// <param name="location">Where the command lives.</param>
    /// <returns>The property name, or null when the identity cannot be stated reliably.</returns>
    public string? Read(INamedTypeSymbol command, string location)
    {
        var properties = command.DeclaredProperties().ToArray();
        if (command.FindInterface(Provider) is { } provider)
        {
            var contract = provider.GetMembers("GetEventSourceId").OfType<IMethodSymbol>().SingleOrDefault();
            var implementation = contract is null ? null : command.FindImplementationForInterfaceMember(contract) as IMethodSymbol;
            var declaration = implementation?.DeclaringSyntaxReferences is [var reference]
                ? reference.GetSyntax() as MethodDeclarationSyntax
                : null;
            var expression = declaration?.ExpressionBody?.Expression ??
                (declaration?.Body?.Statements is [ReturnStatementSyntax returned] ? returned.Expression : null);
            var model = expression is null ? null : models.For(expression.SyntaxTree);
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }

            if (expression is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } &&
                model?.GetSymbolInfo(expression).Symbol is IPropertySymbol property &&
                properties.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, property)) && IsRequiredScalar(property.Type))
            {
                return property.Name;
            }

            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnreadableCommandIdentifier,
                $"The identity provided by '{command.Name}' is not a directly returned required scalar property, so no identifier was stated",
                location);
            return null;
        }

        var candidates = properties.Where(property => HasKeyAttribute(property) || IsEventSourceId(property.Type)).ToArray();
        if (candidates.Length > 1)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.AmbiguousCommandIdentifier,
                $"The command '{command.Name}' has several identity candidates ({string.Join(", ", candidates.Select(property => property.Name))}), so no identifier was stated",
                location);
            return null;
        }

        if (candidates is not [var candidate])
        {
            return null;
        }

        if (IsRequiredScalar(candidate.Type))
        {
            return candidate.Name;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnreadableCommandIdentifier,
            $"The identity property '{command.Name}.{candidate.Name}' is not a required scalar, so no identifier was stated",
            location);
        return null;
    }

    static bool HasKeyAttribute(IPropertySymbol property) =>
        property.GetAttributes().Any(attribute => attribute.AttributeClass.Is(Key)) ||
        property.ContainingType.InstanceConstructors
            .SelectMany(constructor => constructor.Parameters)
            .Any(parameter =>
                parameter.Name == property.Name &&
                SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type) &&
                parameter.GetAttributes().Any(attribute => attribute.AttributeClass.Is(Key)));

    static bool IsEventSourceId(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Is(EventSourceId) || current.Is(GenericEventSourceId))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsRequiredScalar(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;
        var underlying = UnderlyingTypes.Of(type, ref optional, ref collection);
        var scalar = underlying.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments[0] ?? underlying;

        return !optional && !collection &&
            (scalar.TypeKind == TypeKind.Enum || (scalar is INamedTypeSymbol named &&
                ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out _)));
    }
}
