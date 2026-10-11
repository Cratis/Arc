// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
    static readonly string[] _artifactAttributeNames =
    [
        "Cratis.Arc.Queries.ModelBound.ReadModelAttribute",
        "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
        "Microsoft.AspNetCore.Mvc.ControllerAttribute"
    ];

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
        context.RegisterCompilationStartAction(start =>
        {
            var generatedAttribute = start.Compilation.GetTypeByMetadataName("Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute");
            var commandAttribute = start.Compilation.GetTypeByMetadataName("Cratis.Arc.Commands.ModelBound.CommandAttribute");
            var generatable = start.Compilation.GetTypeByMetadataName("Cratis.Concepts.IGeneratable`1");
            if (generatedAttribute is null || commandAttribute is null || generatable is null)
            {
                return;
            }

            var controllerBase = start.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            var artifactAttributes = _artifactAttributeNames.Select(start.Compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>().ToImmutableArray();
            var analysis = new CompilationAnalysis(generatedAttribute, commandAttribute, generatable, controllerBase, artifactAttributes);
            start.RegisterSymbolAction(analysis.AnalyzeCommand, SymbolKind.NamedType);
            start.RegisterSyntaxNodeAction(analysis.AnalyzeDeclaration, SyntaxKind.Attribute);
        });
    }

    static IMethodSymbol? FindMethod(INamedTypeSymbol type, string name)
    {
        var methods = new List<IMethodSymbol>();
        for (var candidate = type; candidate is not null; candidate = candidate.BaseType)
        {
            methods.AddRange(candidate.GetMembers(name).OfType<IMethodSymbol>().Where(method =>
                method.MethodKind == MethodKind.Ordinary && !method.IsStatic &&
                (SymbolEqualityComparer.Default.Equals(candidate, type) || method.DeclaredAccessibility != Accessibility.Private) &&
                !methods.Exists(derived => SameSignature(derived, method))));
        }

        // Reflection's name-only GetMethod is ambiguous when more than one signature remains.
        return methods.Count == 1 ? methods[0] : null;
    }

    static bool SameSignature(IMethodSymbol first, IMethodSymbol second) =>
        first.Arity == second.Arity && first.Parameters.Length == second.Parameters.Length &&
        first.Parameters.Zip(second.Parameters, (left, right) =>
            IsByReference(left) == IsByReference(right) &&
            SymbolEqualityComparer.Default.Equals(left.Type, right.Type)).All(equal => equal);

    static bool IsByReference(IParameterSymbol parameter) => parameter.RefKind != RefKind.None;

    static bool HasNullableReadState(IParameterSymbol parameter)
    {
        if (parameter.Type.IsValueType)
        {
            return parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        }

        var attributes = parameter.GetAttributes().Where(attribute => attribute.AttributeClass is { } type && Namespace(type) == "System.Diagnostics.CodeAnalysis")
            .Select(attribute => attribute.AttributeClass!.Name).ToArray();
        if (attributes.Contains("NotNullAttribute", StringComparer.Ordinal))
        {
            return false;
        }

        return attributes.Contains("MaybeNullAttribute", StringComparer.Ordinal) ||
            attributes.Contains("MaybeNullWhenAttribute", StringComparer.Ordinal) ||
            (parameter.OriginalDefinition.NullableAnnotation != NullableAnnotation.None &&
                parameter.NullableAnnotation == NullableAnnotation.Annotated);
    }

    static string NameFor(IParameterSymbol parameter, AttributeData attribute) =>
        attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name;

    static Location LocationOf(IParameterSymbol parameter, AttributeData attribute, CancellationToken cancellationToken) =>
        attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? parameter.Locations.FirstOrDefault() ?? Location.None;

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
        // A non-null Nullable<T> is boxed as T before the runtime unwraps or flattens it.
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            return UnwrapAlternatives(nullable.TypeArguments[0]);
        }

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
        (source is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
            IsAssignableTo(nullable.TypeArguments[0], destination)) ||
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

    sealed class CompilationAnalysis(
        INamedTypeSymbol generatedAttribute,
        INamedTypeSymbol commandAttribute,
        INamedTypeSymbol generatable,
        INamedTypeSymbol? controllerBase,
        ImmutableArray<INamedTypeSymbol> artifactAttributes)
    {
        readonly ConcurrentDictionary<IParameterSymbol, ConcurrentDictionary<string, byte>> _reported = new(SymbolEqualityComparer.Default);

        internal void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var syntax = (AttributeSyntax)context.Node;
            if (syntax.Parent?.Parent is ParameterSyntax parameterSyntax &&
                context.SemanticModel.GetSymbolInfo(syntax, context.CancellationToken).Symbol is IMethodSymbol constructor &&
                SymbolEqualityComparer.Default.Equals(constructor.ContainingType, generatedAttribute) &&
                context.SemanticModel.GetDeclaredSymbol(parameterSyntax, context.CancellationToken) is IParameterSymbol parameter &&
                AttributeOf(parameter) is { } attribute &&
                (parameter.ContainingSymbol is not IMethodSymbol method || method.Name != "Handle" || method.IsStatic ||
                    IsUnsupportedArtifact(method.ContainingType)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARC0023_GeneratedValueOutsideHandle,
                    LocationOf(parameter, attribute, context.CancellationToken),
                    parameter.Name));
            }
        }

        internal void AnalyzeCommand(SymbolAnalysisContext context)
        {
            var command = (INamedTypeSymbol)context.Symbol;
            if (!IsCommand(command) || FindMethod(command, "Handle") is not { } handle)
            {
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var providedTypes = FindMethod(command, "Provide") is { } provide
                ? ProducedTypes(provide.ReturnType).Where(type => !IsControlType(type)).ToArray()
                : [];
            foreach (var declaration in handle.Parameters.Select(parameter => (Parameter: parameter, Attribute: EffectiveAttributeOf(parameter)))
                .Where(declaration => declaration.Attribute is not null))
            {
                AnalyzeParameter(context, command, declaration.Parameter, declaration.Attribute!, names, providedTypes);
            }
        }

        void AnalyzeParameter(
            SymbolAnalysisContext context,
            INamedTypeSymbol command,
            IParameterSymbol parameter,
            AttributeData attribute,
            HashSet<string> names,
            ITypeSymbol[] providedTypes)
        {
            var location = parameter.ContainingSymbol is IMethodSymbol { IsOverride: true }
                ? parameter.Locations.FirstOrDefault() ?? Location.None
                : LocationOf(parameter, attribute, context.CancellationToken);
            if (parameter.Type is not ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Type } && (!IsGeneratable(parameter) || HasNullableReadState(parameter)))
            {
                ReportOnce(context, parameter, Diagnostic.Create(DiagnosticDescriptors.ARC0022_InvalidGeneratedValueType, location, parameter.Name, parameter.Type.Name));
            }

            var name = NameFor(parameter, attribute);
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
            {
                ReportOnce(context, parameter, Diagnostic.Create(DiagnosticDescriptors.ARC0024_InvalidGeneratedValueName, location, parameter.Name, command.Name));
            }

            if (providedTypes.Any(type => IsAssignableTo(type, parameter.Type)))
            {
                ReportOnce(context, parameter, Diagnostic.Create(DiagnosticDescriptors.ARC0025_ProvidedGeneratedValue, location, parameter.Name, parameter.Type.Name, command.Name));
            }
        }

        bool IsUnsupportedArtifact(INamedTypeSymbol type) =>
            SymbolEqualityComparer.Default.Equals(type, controllerBase) ||
            type.GetAttributes().Any(attribute => artifactAttributes.Any(expected => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, expected))) ||
            (type.BaseType is not null && IsUnsupportedArtifact(type.BaseType));

        void ReportOnce(SymbolAnalysisContext context, IParameterSymbol parameter, Diagnostic diagnostic)
        {
            var reported = _reported.GetOrAdd(parameter.OriginalDefinition, static _ => new(StringComparer.Ordinal));
            if (reported.TryAdd(diagnostic.Id, 0))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        bool IsGeneratable(IParameterSymbol parameter) =>
            parameter.RefKind == RefKind.None && parameter.Type.AllInterfaces.Any(contract =>
                SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, generatable) &&
                SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], parameter.Type));

        AttributeData? EffectiveAttributeOf(IParameterSymbol parameter) =>
            AttributeOf(parameter) ??
            (parameter.ContainingSymbol is IMethodSymbol { OverriddenMethod: { } overridden }
                ? EffectiveAttributeOf(overridden.Parameters[parameter.Ordinal])
                : null);

        AttributeData? AttributeOf(IParameterSymbol parameter) =>
            parameter.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, generatedAttribute));

        bool IsCommand(INamedTypeSymbol type) =>
            type.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)) ||
            (type.BaseType is not null && IsCommand(type.BaseType));
    }
}
