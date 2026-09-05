// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that flags a [Roles] argument declared as a string literal instead of a nameof expression.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RolesLiteralAnalyzer : DiagnosticAnalyzer
{
    const string RolesAttributeName = "Cratis.Arc.Authorization.RolesAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARC0011_RolesArgumentShouldUseNameof];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (attribute.ArgumentList is not { } argumentList || argumentList.Arguments.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol constructor ||
            constructor.ContainingType?.ToDisplayString() != RolesAttributeName)
        {
            return;
        }

        foreach (var argument in argumentList.Arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARC0011_RolesArgumentShouldUseNameof,
                    literal.GetLocation(),
                    literal.Token.ValueText));
            }
        }
    }
}
