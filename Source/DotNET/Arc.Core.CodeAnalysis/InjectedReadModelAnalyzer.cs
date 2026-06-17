// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer for command-scoped read model injection.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InjectedReadModelAnalyzer : DiagnosticAnalyzer
{
    const string ReadModelAttribute = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string CommandValidatorNamespace = "Cratis.Arc.Commands";
    const string ProjectionForType = "Cratis.Chronicle.Projections.IProjectionFor`1";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.ARC0006_CommandScopedReadModelCanBeMissing
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var projectionForType = compilationContext.Compilation.GetTypeByMetadataName(ProjectionForType);
            var projectionReadModels = new Lazy<ImmutableHashSet<ITypeSymbol>>(() =>
                projectionForType is null
                    ? []
                    : FindProjectionReadModels(compilationContext.Compilation.Assembly.GlobalNamespace, projectionForType));

            compilationContext.RegisterSymbolAction(context => AnalyzeNamedType(context, projectionReadModels), SymbolKind.NamedType);
        });
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context, Lazy<ImmutableHashSet<ITypeSymbol>> projectionReadModels)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (HasAttribute(type, CommandAttribute))
        {
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(IsCommandMethod))
            {
                AnalyzeParameters(context, method.Parameters, $"{type.Name}.{method.Name}", projectionReadModels);
            }
        }

        if (IsCommandValidator(type))
        {
            foreach (var constructor in type.Constructors.Where(_ => !_.IsStatic))
            {
                AnalyzeParameters(context, constructor.Parameters, type.Name, projectionReadModels);
            }
        }
    }

    static void AnalyzeParameters(
        SymbolAnalysisContext context,
        ImmutableArray<IParameterSymbol> parameters,
        string owner,
        Lazy<ImmutableHashSet<ITypeSymbol>> projectionReadModels)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.NullableAnnotation == NullableAnnotation.Annotated ||
                !IsReadModel(parameter.Type, projectionReadModels))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0006_CommandScopedReadModelCanBeMissing,
                parameter.Locations[0],
                parameter.Type.Name,
                owner));
        }
    }

    static bool IsCommandMethod(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        !method.IsStatic &&
        method.DeclaredAccessibility == Accessibility.Public &&
        (method.Name == "Handle" || method.Name == "Provide");

    static bool IsCommandValidator(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "CommandValidator" &&
                current.ContainingNamespace?.ToDisplayString() == CommandValidatorNamespace)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsReadModel(ITypeSymbol type, Lazy<ImmutableHashSet<ITypeSymbol>> projectionReadModels) =>
        type is INamedTypeSymbol namedType &&
        (HasAttribute(namedType, ReadModelAttribute) || projectionReadModels.Value.Contains(type));

    static ImmutableHashSet<ITypeSymbol> FindProjectionReadModels(INamespaceSymbol rootNamespace, INamedTypeSymbol projectionForType)
    {
        var readModels = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var type in GetAllTypes(rootNamespace))
        {
            foreach (var @interface in type.AllInterfaces.Where(_ =>
                SymbolEqualityComparer.Default.Equals(_.OriginalDefinition, projectionForType) &&
                _.TypeArguments.Length == 1))
            {
                readModels.Add(@interface.TypeArguments[0]);
            }
        }

        return readModels.ToImmutable();
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            foreach (var nestedType in GetAllTypes(type))
            {
                yield return nestedType;
            }
        }

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var typeInNestedType in GetAllTypes(nestedType))
            {
                yield return typeInNestedType;
            }
        }
    }

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName) =>
        typeSymbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeFullName);
}
