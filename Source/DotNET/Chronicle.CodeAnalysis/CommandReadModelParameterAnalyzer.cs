// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Suggests model parameters instead of direct reader or token dependencies in command handlers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandReadModelParameterAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0040_PreferReadModelParameter];

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
        if (context.Compilation.AssemblyName == "Cratis.Arc.Chronicle" || syntax.Parent?.Parent is not MethodDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not IParameterSymbol parameter ||
            parameter.ContainingSymbol is not IMethodSymbol { Name: "Handle", IsStatic: false, DeclaredAccessibility: Accessibility.Public } method)
        {
            return;
        }

        if (!RawGuidResponseAnalysis.HasAttribute(method.ContainingType, "Cratis.Arc.Commands.ModelBound.CommandAttribute", context.Compilation) ||
            !IsDecisionDependency(parameter.Type, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0040_PreferReadModelParameter,
            syntax.GetLocation(),
            parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    static bool IsDecisionDependency(ITypeSymbol type, Compilation compilation) =>
        RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.ReadModels.IDecisionReads", compilation) ||
        RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.ReadModels.DecisionRead`1", compilation);
}
