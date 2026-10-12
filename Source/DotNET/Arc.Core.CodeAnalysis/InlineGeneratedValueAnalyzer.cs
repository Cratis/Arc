// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Suggests scenario-pinnable parameters instead of inline concept generation in Handle.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InlineGeneratedValueAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARC0028_InlineGeneratedValue];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var method = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is null || !InlineGeneratedValue.IsCommandHandle(method, context.SemanticModel) ||
            invocation.Ancestors().TakeWhile(node => node != method).Any(InlineGeneratedValue.IsNestedFunction) ||
            InlineGeneratedValue.ExpressionFor(invocation, context.SemanticModel) is not { } expression || expression.Parent is ExpressionStatementSyntax)
        {
            return;
        }

        var type = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
        var concept = InlineGeneratedValue.IsConcept(type.Type) ? type.Type! : type.ConvertedType!;
        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARC0028_InlineGeneratedValue, expression.GetLocation(), concept.Name));
    }
}
