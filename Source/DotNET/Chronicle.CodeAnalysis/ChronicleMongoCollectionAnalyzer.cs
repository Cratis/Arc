// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Advises when an injected MongoDB collection contains a recognizable Chronicle-backed model.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChronicleMongoCollectionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0026_ChronicleModelThroughMongoCollection];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backedTypes = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => ChronicleStateSymbols.BackedTypes(start.Compilation));
            start.RegisterSyntaxNodeAction(context => AnalyzeParameter(context, backedTypes), SyntaxKind.Parameter);
        });
    }

    static void AnalyzeParameter(SyntaxNodeAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var syntax = (ParameterSyntax)context.Node;
        if (syntax.Parent?.Parent is not BaseMethodDeclarationSyntax and not TypeDeclarationSyntax ||
            context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not IParameterSymbol parameter ||
            parameter.Type is not INamedTypeSymbol collection ||
            parameter.ContainingSymbol is not IMethodSymbol method ||
            !IsCommandInjection(method, context.Compilation))
        {
            return;
        }
        var model = collection.AllInterfaces.Prepend(collection)
            .FirstOrDefault(type => RawGuidResponseAnalysis.IsType(type, "MongoDB.Driver.IMongoCollection`1", context.Compilation))?.TypeArguments[0];
        if (model is null || !ChronicleStateSymbols.IsBacked(model, context.Compilation, backedTypes))
        {
            return;
        }
        context.ReportDiagnostic(ChronicleStateSymbols.Diagnostic(
            DiagnosticDescriptors.ARCCHR0026_ChronicleModelThroughMongoCollection,
            parameter,
            DiagnosticSeverity.Info,
            parameter.Name,
            model.Name));
    }

    static bool IsCommandInjection(IMethodSymbol method, Compilation compilation)
    {
        if (RawGuidResponseAnalysis.HasAttribute(method.ContainingType, "Cratis.Arc.Commands.ModelBound.CommandAttribute", compilation))
        {
            return ChronicleStateSymbols.DependencyMethods(method.ContainingType).Contains(method, SymbolEqualityComparer.Default);
        }
        if (method.MethodKind != MethodKind.Constructor)
        {
            return false;
        }
        for (var current = method.ContainingType.BaseType; current is not null; current = current.BaseType)
        {
            if (RawGuidResponseAnalysis.IsType(current, "Cratis.Arc.Commands.CommandValidator`1", compilation))
            {
                return true;
            }
        }
        return false;
    }
}
