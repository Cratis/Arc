// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a command has multiple event source id candidates but does not declare which one to use.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string EventsNamespace = "Cratis.Chronicle.Events";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";
    const string KeysNamespace = "Cratis.Chronicle.Keys";
    const string EventSourceIdTypeName = "EventSourceId";
    const string KeyAttributeName = "KeyAttribute";
    const string CanProvideEventSourceIdName = "ICanProvideEventSourceId";
    const string EventForEventSourceIdName = "EventForEventSourceId";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0002_AmbiguousCommandEventSourceId];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (namedTypeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!HasAttribute(namedTypeSymbol, CommandAttributeName))
        {
            return;
        }

        if (ImplementsCanProvideEventSourceId(namedTypeSymbol))
        {
            return;
        }

        var candidates = FindEventSourceIdCandidates(namedTypeSymbol).ToArray();

        if (candidates.Length < 2)
        {
            return;
        }

        if (HandleReturnsExplicitEventSource(namedTypeSymbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0002_AmbiguousCommandEventSourceId,
            namedTypeSymbol.Locations[0],
            namedTypeSymbol.Name,
            string.Join(", ", candidates.Select(candidate => candidate.Name))));
    }

    static IEnumerable<IPropertySymbol> FindEventSourceIdCandidates(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property =>
                !property.IsStatic &&
                property.DeclaredAccessibility == Accessibility.Public &&
                IsEventSourceIdCandidate(property));

    static bool IsEventSourceIdCandidate(IPropertySymbol property) =>
        HasAttribute(property, KeysNamespace, KeyAttributeName) ||
        IsOrDerivesFromEventSourceId(property.Type) ||
        HasImplicitConversionToEventSourceId(property.Type);

    static bool IsOrDerivesFromEventSourceId(ITypeSymbol? type)
    {
        var current = type;
        while (current is not null)
        {
            if (current.Name == EventSourceIdTypeName &&
                current.ContainingNamespace?.ToDisplayString() == EventsNamespace)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    static bool HasImplicitConversionToEventSourceId(ITypeSymbol? type)
    {
        var current = type;
        while (current is not null)
        {
            var hasConversion = current.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(method =>
                    method.MethodKind == MethodKind.Conversion &&
                    method.Name == "op_Implicit" &&
                    IsOrDerivesFromEventSourceId(method.ReturnType));

            if (hasConversion)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    static bool ImplementsCanProvideEventSourceId(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == CanProvideEventSourceIdName &&
            @interface.ContainingNamespace?.ToDisplayString() == EventsNamespace);

    static bool HandleReturnsExplicitEventSource(INamedTypeSymbol typeSymbol)
    {
        var handleMethods = typeSymbol.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.MethodKind == MethodKind.Ordinary &&
                method.DeclaredAccessibility == Accessibility.Public &&
                !method.IsStatic)
            .ToArray();

        return handleMethods.Length > 0 && handleMethods.All(ReturnsExplicitEventSource);
    }

    static bool ReturnsExplicitEventSource(IMethodSymbol method)
    {
        var returnType = UnwrapTask(method.ReturnType);
        if (returnType is null)
        {
            return false;
        }

        if (IsEventForEventSourceId(returnType) || IsSequenceOfEventForEventSourceId(returnType))
        {
            return true;
        }

        return TupleProvidesExplicitEventSource(returnType);
    }

    static bool TupleProvidesExplicitEventSource(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return false;
        }

        var elements = tuple.TupleElements;

        // A returned or generated EventForEventSourceId (or a sequence of them) carries its own event source id,
        // regardless of which position it sits in the tuple.
        if (elements.Any(element => IsEventForEventSourceId(element.Type) || IsSequenceOfEventForEventSourceId(element.Type)))
        {
            return true;
        }

        // New-stream create: the first element is the event source id — a concept deriving from EventSourceId<T> —
        // so the event source is the returned/generated id rather than a command property. ICanProvideEventSourceId
        // is neither needed nor implementable there.
        return elements.Length > 0 && IsOrDerivesFromEventSourceId(elements[0].Type);
    }

    static ITypeSymbol? UnwrapTask(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType &&
            namedType.Name == "Task" &&
            namedType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
        {
            return namedType.TypeArguments.Length == 1 ? namedType.TypeArguments[0] : null;
        }

        return returnType;
    }

    static bool IsEventForEventSourceId(ITypeSymbol type) =>
        type.Name == EventForEventSourceIdName &&
        type.ContainingNamespace?.ToDisplayString() == EventSequencesNamespace;

    static bool IsSequenceOfEventForEventSourceId(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return IsEventForEventSourceId(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments.Length == 1 && IsEventForEventSourceId(namedType.TypeArguments[0]);
        }

        return false;
    }

    static bool HasAttribute(ISymbol symbol, string fullyQualifiedName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == fullyQualifiedName);

    static bool HasAttribute(ISymbol symbol, string @namespace, string typeName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == typeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == @namespace);
}
