// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a project sets up Arc with <c>AddCratisArc</c> but never wires Chronicle with
/// <c>WithChronicle</c> or <c>AddCratis</c>, yet uses Chronicle: aggregate roots, reactors, reducers, fluent or
/// model-bound projections, event types, or a type that injects a Chronicle service such as <c>IEventLog</c>.
/// </summary>
/// <remarks>
/// The report is intentionally scoped to a single compilation: it fires only when the setup call and the
/// Chronicle artifacts live in the same project. This keeps it free of false positives — a domain project that
/// only defines artifacts (setup happens in a separate host) and a host project that only calls
/// <c>AddCratisArc</c> (artifacts live elsewhere) both stay silent.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingWithChronicleAnalyzer : DiagnosticAnalyzer
{
    const string SetupMethodName = "AddCratisArc";
    const string ChronicleNamespacePrefix = "Cratis.Chronicle";
    const string AggregateRootName = "AggregateRoot";
    const string AggregateRootNamespace = "Cratis.Arc.Chronicle.Aggregates";

    static readonly string[] _wiringMethodNames = ["WithChronicle", "AddCratis"];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARCCHR0005_ChronicleArtifactsWithoutWithChronicle];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var setupCalls = new ConcurrentBag<Location>();
            var wiringCalls = new ConcurrentBag<bool>();

            compilationStart.RegisterOperationAction(
                operationContext =>
                {
                    var method = ((IInvocationOperation)operationContext.Operation).TargetMethod;
                    if (!method.IsExtensionMethod)
                    {
                        return;
                    }

                    if (method.Name == SetupMethodName)
                    {
                        setupCalls.Add(operationContext.Operation.Syntax.GetLocation());
                    }
                    else if (_wiringMethodNames.Contains(method.Name))
                    {
                        wiringCalls.Add(true);
                    }
                },
                OperationKind.Invocation);

            compilationStart.RegisterCompilationEndAction(compilationEnd =>
            {
                if (!wiringCalls.IsEmpty || setupCalls.IsEmpty)
                {
                    return;
                }

                var artifact = FindChronicleUsage(compilationEnd.Compilation);
                if (artifact is null)
                {
                    return;
                }

                foreach (var location in setupCalls)
                {
                    compilationEnd.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ARCCHR0005_ChronicleArtifactsWithoutWithChronicle,
                        location,
                        artifact));
                }
            });
        });
    }

    static string? FindChronicleUsage(Compilation compilation)
    {
        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace))
        {
            if (IsAggregateRoot(type) ||
                ImplementsChronicleInterface(type) ||
                HasChronicleAttribute(type) ||
                InjectsChronicleService(type))
            {
                return type.Name;
            }
        }

        return null;
    }

    static bool IsAggregateRoot(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == AggregateRootName &&
                current.ContainingNamespace?.ToDisplayString() == AggregateRootNamespace)
            {
                return true;
            }
        }

        return false;
    }

    static bool ImplementsChronicleInterface(INamedTypeSymbol type) =>
        type.AllInterfaces.Any(IsInChronicleNamespace);

    static bool HasChronicleAttribute(INamedTypeSymbol type) =>
        HasChronicleAttribute(type.GetAttributes()) ||
        type.GetMembers().OfType<IPropertySymbol>().Any(property => HasChronicleAttribute(property.GetAttributes()));

    static bool HasChronicleAttribute(ImmutableArray<AttributeData> attributes) =>
        attributes.Any(attribute => attribute.AttributeClass is { } attributeClass && IsInChronicleNamespace(attributeClass));

    static bool InjectsChronicleService(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Any(parameter =>
                parameter.Type.TypeKind == TypeKind.Interface && IsInChronicleNamespace(parameter.Type)));

    static bool IsInChronicleNamespace(ISymbol symbol)
    {
        var @namespace = symbol.ContainingNamespace?.ToDisplayString();
        return @namespace == ChronicleNamespacePrefix ||
            @namespace?.StartsWith(ChronicleNamespacePrefix + ".", StringComparison.Ordinal) == true;
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            foreach (var nested in GetAllTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var childNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(childNamespace))
            {
                yield return type;
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var nestedChild in GetAllTypes(nested))
            {
                yield return nestedChild;
            }
        }
    }
}
