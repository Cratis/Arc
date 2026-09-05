// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a reactor handler invoking <c>ICommandPipeline.Execute</c> is not marked with <c>[OnceOnly]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorCommandPipelineExecuteOnceOnlyAnalyzer : DiagnosticAnalyzer
{
    const string ExecuteMethodName = "Execute";
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";
    const string OnceOnlyAttributeName = "OnceOnlyAttribute";
    const string CommandPipelineInterfaceName = "ICommandPipeline";
    const string CommandsNamespace = "Cratis.Arc.Commands";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0006_ReactorCommandPipelineExecuteMustBeOnceOnly];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.Name != ExecuteMethodName || !IsCommandPipeline(methodSymbol.ContainingType))
        {
            return;
        }

        var methodDeclaration = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol enclosingMethod)
        {
            return;
        }

        if (!IsReactor(enclosingMethod.ContainingType))
        {
            return;
        }

        if (HasOnceOnlyAttribute(enclosingMethod) || HasOnceOnlyAttribute(enclosingMethod.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0006_ReactorCommandPipelineExecuteMustBeOnceOnly,
            invocation.GetLocation(),
            enclosingMethod.Name));
    }

    static bool IsReactor(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == ReactorInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool IsCommandPipeline(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        if (IsCommandPipelineInterface(typeSymbol))
        {
            return true;
        }

        return typeSymbol.AllInterfaces.Any(IsCommandPipelineInterface);
    }

    static bool IsCommandPipelineInterface(ITypeSymbol typeSymbol) =>
        typeSymbol.Name == CommandPipelineInterfaceName &&
        typeSymbol.ContainingNamespace?.ToDisplayString() == CommandsNamespace;

    static bool HasOnceOnlyAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == OnceOnlyAttributeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);
}
