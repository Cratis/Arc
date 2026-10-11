// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Checks generated value declarations against the model-bound command parameter contract.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GeneratedValueAnalyzer : DiagnosticAnalyzer
{
    const string GeneratedAttribute = "Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute";
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string GeneratableInterface = "Cratis.Concepts.IGeneratable`1";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARC0022_InvalidGeneratedValueType,
        DiagnosticDescriptors.ARC0023_GeneratedValueOutsideHandle,
        DiagnosticDescriptors.ARC0024_InvalidGeneratedValueName,
        DiagnosticDescriptors.ARC0025_ProvidedGeneratedValue
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var syntax = (ParameterSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not IParameterSymbol parameter ||
            GeneratedAttributeOf(parameter) is not { } attribute)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? syntax.GetLocation();
        if (parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: false, Name: "Handle" } method ||
            !IsCommand(method.ContainingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARC0023_GeneratedValueOutsideHandle, location, parameter.Name));
            return;
        }

        if (!IsGeneratable(parameter, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARC0022_InvalidGeneratedValueType, location, parameter.Name, parameter.Type.Name));
        }

        var name = NameFor(parameter, attribute);
        if (string.IsNullOrWhiteSpace(name) || method.Parameters.Take(parameter.Ordinal)
            .Any(previous => GeneratedAttributeOf(previous) is { } previousAttribute &&
                string.Equals(NameFor(previous, previousAttribute), name, StringComparison.Ordinal)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARC0024_InvalidGeneratedValueName, location, parameter.Name, method.ContainingType.Name));
        }

        if (FindProvide(method.ContainingType) is { } provide &&
            ProducedTypes(provide.ReturnType).Any(type => !IsControlType(type) && IsAssignableTo(type, parameter.Type)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARC0025_ProvidedGeneratedValue, location, parameter.Name, parameter.Type.Name, method.ContainingType.Name));
        }
    }

    static bool IsGeneratable(IParameterSymbol parameter, Compilation compilation) =>
        parameter.RefKind == RefKind.None &&
        parameter.NullableAnnotation != NullableAnnotation.Annotated &&
        parameter.Type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } &&
        parameter.Type.AllInterfaces.Any(contract =>
            SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, compilation.GetTypeByMetadataName(GeneratableInterface)) &&
            SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], parameter.Type));

    static AttributeData? GeneratedAttributeOf(IParameterSymbol parameter) =>
        parameter.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == GeneratedAttribute);

    static string NameFor(IParameterSymbol parameter, AttributeData attribute) =>
        attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name;

    static bool IsCommand(INamedTypeSymbol type) =>
        type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == CommandAttribute) ||
        (type.BaseType is not null && IsCommand(type.BaseType));

    static IMethodSymbol? FindProvide(INamedTypeSymbol type)
    {
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            var method = candidate.GetMembers("Provide").OfType<IMethodSymbol>().FirstOrDefault(method =>
                method.MethodKind == MethodKind.Ordinary && !method.IsStatic &&
                (SymbolEqualityComparer.Default.Equals(candidate, type) || method.DeclaredAccessibility != Accessibility.Private));
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    static IEnumerable<ITypeSymbol> ProducedTypes(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && Namespace(named) == "System.Threading.Tasks" && (named.Name == "Task" || named.Name == "ValueTask"))
        {
            return named.TypeArguments.Length == 1 ? FlattenTypes(named.TypeArguments[0]) : [];
        }

        return FlattenTypes(type);
    }

    static IEnumerable<ITypeSymbol> FlattenTypes(ITypeSymbol type) =>
        UnwrapAlternatives(type).SelectMany(alternative => TupleElements(alternative) is { } elements
            ? elements.SelectMany(UnwrapAlternatives)
            : [alternative]);

    static IEnumerable<ITypeSymbol>? TupleElements(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.Select(element => element.Type);
        }

        if (type is INamedTypeSymbol named && named.Name == "Tuple" && Namespace(named) == "System")
        {
            return named.TypeArguments.Length == 8
                ? named.TypeArguments.Take(7).Concat(TupleElements(named.TypeArguments[7]) ?? [named.TypeArguments[7]])
                : named.TypeArguments;
        }

        return null;
    }

    static IEnumerable<ITypeSymbol> UnwrapAlternatives(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || !named.AllInterfaces.Any(contract => contract.Name == "IOneOf" && Namespace(contract) == "OneOf"))
        {
            return [type];
        }

        for (var candidate = named; candidate is not null; candidate = candidate.BaseType)
        {
            if (Namespace(candidate) == "OneOf" && (candidate.Name == "OneOf" || candidate.Name == "OneOfBase"))
            {
                return candidate.TypeArguments.SelectMany(UnwrapAlternatives);
            }
        }

        return named.TypeArguments.SelectMany(UnwrapAlternatives);
    }

    static bool IsAssignableTo(ITypeSymbol source, ITypeSymbol destination) =>
        SymbolEqualityComparer.Default.Equals(source, destination) ||
        source.AllInterfaces.Any(contract => SymbolEqualityComparer.Default.Equals(contract, destination)) ||
        (source.BaseType is not null && IsAssignableTo(source.BaseType, destination));

    static bool IsControlType(ITypeSymbol type) =>
        IsNamed(type, "Cratis.Arc.Commands", "CommandResult") ||
        IsNamed(type, "Cratis.Arc.Authorization", "AuthorizationResult") ||
        IsNamed(type, "Cratis.Arc.Validation", "ValidationResult") ||
        type.AllInterfaces.Any(contract => IsNamed(contract, "System.Collections.Generic", "IEnumerable") &&
            contract.TypeArguments.Length == 1 && IsNamed(contract.TypeArguments[0], "Cratis.Arc.Validation", "ValidationResult"));

    static bool IsNamed(ITypeSymbol type, string @namespace, string name) => type.Name == name && Namespace(type) == @namespace;

    static string Namespace(ITypeSymbol type) => type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
}
