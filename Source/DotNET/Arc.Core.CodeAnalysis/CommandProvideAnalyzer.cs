// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer for the optional Provide method on a Command type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandProvideAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            DiagnosticDescriptors.ARC0005_ProvidedValueNotConsumed
        ];

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
        if (!HasAttribute(namedTypeSymbol, "Cratis.Arc.Commands.ModelBound.CommandAttribute"))
        {
            return;
        }

        var handleMethod = FindMethod(namedTypeSymbol, "Handle");
        var provideMethod = FindMethod(namedTypeSymbol, "Provide");
        if (handleMethod is null || provideMethod is null)
        {
            return;
        }

        var handleParameterTypes = handleMethod.Parameters.Select(parameter => parameter.Type).ToArray();

        foreach (var providedType in GetProducedTypes(provideMethod.ReturnType))
        {
            if (IsControlType(providedType))
            {
                continue;
            }

            if (handleParameterTypes.Any(parameterType => IsAssignableTo(context.Compilation, providedType, parameterType)))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0005_ProvidedValueNotConsumed,
                provideMethod.Locations[0],
                providedType.Name,
                namedTypeSymbol.Name));
        }
    }

    static IEnumerable<ITypeSymbol> GetProducedTypes(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return [type];
        }

        var ns = Namespace(named);
        if (ns == "System.Threading.Tasks" && (named.Name == "Task" || named.Name == "ValueTask"))
        {
            return named.TypeArguments.Length == 1 ? GetProducedTypes(named.TypeArguments[0]) : [];
        }

        if (named.IsTupleType)
        {
            return named.TupleElements.SelectMany(element => GetProducedTypes(element.Type));
        }

        if (IsOneOf(named))
        {
            return named.TypeArguments.SelectMany(GetProducedTypes);
        }

        return [type];
    }

    static bool IsControlType(ITypeSymbol type)
    {
        if (IsNamed(type, "Cratis.Arc.Validation", "ValidationResult") ||
            IsNamed(type, "Cratis.Arc.Authorization", "AuthorizationResult") ||
            IsNamed(type, "Cratis.Arc.Commands", "CommandResult"))
        {
            return true;
        }

        var interfaces = type is INamedTypeSymbol named ? named.AllInterfaces.Add(named) : type.AllInterfaces;
        return interfaces.Any(@interface =>
            @interface.Name == "IEnumerable" &&
            @interface.TypeArguments.Length == 1 &&
            IsNamed(@interface.TypeArguments[0], "Cratis.Arc.Validation", "ValidationResult"));
    }

    static bool IsAssignableTo(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
    {
        var conversion = compilation.ClassifyCommonConversion(source, destination);
        return conversion.IsIdentity || conversion.IsImplicit;
    }

    static bool IsOneOf(INamedTypeSymbol type) =>
        type.TypeArguments.Length > 0 &&
        type.AllInterfaces.Any(@interface => @interface.Name == "IOneOf" && Namespace(@interface) == "OneOf");

    static bool IsNamed(ITypeSymbol type, string @namespace, string name) =>
        type.Name == name && Namespace(type) == @namespace;

    static string Namespace(ITypeSymbol type) => type.ContainingNamespace?.ToDisplayString() ?? string.Empty;

    static IMethodSymbol? FindMethod(INamedTypeSymbol typeSymbol, string name) =>
        typeSymbol.GetMembers(name)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.MethodKind == MethodKind.Ordinary && !method.IsStatic);

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName) =>
        typeSymbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeFullName);
}
