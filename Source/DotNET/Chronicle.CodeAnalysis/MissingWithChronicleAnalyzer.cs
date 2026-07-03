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
/// <c>WithChronicle</c> or <c>AddCratis</c>, yet defines Chronicle artifacts such as aggregate roots, reactors,
/// event types, or projections.
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
    const string EventTypeAttribute = "Cratis.Chronicle.Events.EventTypeAttribute";
    const string ProjectionForType = "Cratis.Chronicle.Projections.IProjectionFor`1";
    const string AggregateRootName = "AggregateRoot";
    const string AggregateRootNamespace = "Cratis.Arc.Chronicle.Aggregates";
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";
    const string ReducerInterfaceName = "IReducer";
    const string ReducersNamespace = "Cratis.Chronicle.Reducers";
    const string EventLogInterfaceName = "IEventLog";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";
    const string EventStoreInterfaceName = "IEventStore";
    const string EventStoreNamespace = "Cratis.Chronicle";

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

                var artifact = FindChronicleArtifact(compilationEnd.Compilation);
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

    static string? FindChronicleArtifact(Compilation compilation)
    {
        var eventTypeAttribute = compilation.GetTypeByMetadataName(EventTypeAttribute);
        var projectionForType = compilation.GetTypeByMetadataName(ProjectionForType);

        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace))
        {
            if (IsAggregateRoot(type) ||
                ImplementsMarker(type, ReactorInterfaceName, ReactorsNamespace) ||
                ImplementsMarker(type, ReducerInterfaceName, ReducersNamespace) ||
                HasEventTypeAttribute(type, eventTypeAttribute) ||
                IsProjection(type, projectionForType) ||
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

    static bool ImplementsMarker(INamedTypeSymbol type, string interfaceName, string interfaceNamespace) =>
        type.TypeKind == TypeKind.Class &&
        type.AllInterfaces.Any(@interface =>
            @interface.Name == interfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == interfaceNamespace);

    static bool InjectsChronicleService(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Any(parameter => IsChronicleService(parameter.Type)));

    static bool IsChronicleService(ITypeSymbol type) =>
        (type.Name == EventLogInterfaceName && type.ContainingNamespace?.ToDisplayString() == EventSequencesNamespace) ||
        (type.Name == EventStoreInterfaceName && type.ContainingNamespace?.ToDisplayString() == EventStoreNamespace);

    static bool HasEventTypeAttribute(INamedTypeSymbol type, INamedTypeSymbol? eventTypeAttribute) =>
        eventTypeAttribute is not null &&
        type.GetAttributes().Any(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, eventTypeAttribute));

    static bool IsProjection(INamedTypeSymbol type, INamedTypeSymbol? projectionForType) =>
        projectionForType is not null &&
        type.AllInterfaces.Any(@interface =>
            SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, projectionForType));

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
