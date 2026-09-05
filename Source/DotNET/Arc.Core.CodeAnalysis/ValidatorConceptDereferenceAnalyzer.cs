// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that flags a FluentValidation rule selector that dereferences a member of a possibly-null concept,
/// which throws at validation time instead of producing a validation error.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ValidatorConceptDereferenceAnalyzer : DiagnosticAnalyzer
{
    const string ConceptAsTypeName = "ConceptAs";
    const string ConceptsNamespace = "Cratis.Concepts";
    const string AbstractValidatorTypeName = "AbstractValidator";
    const string FluentValidationNamespace = "FluentValidation";
    const string RuleForMethodName = "RuleFor";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARC0013_ValidatorDereferencesNullConcept];

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

        // Must be a FluentValidation RuleFor(...) call (RuleFor is declared on AbstractValidator<T>).
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
            method.Name != RuleForMethodName ||
            !IsFluentValidator(method.ContainingType))
        {
            return;
        }

        // RuleFor takes a single property-selector lambda, e.g. RuleFor(c => c.Concept.Value).
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        var body = invocation.ArgumentList.Arguments[0].Expression switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Body as ExpressionSyntax,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.Body as ExpressionSyntax,
            _ => null
        };
        if (body is null)
        {
            return;
        }

        foreach (var memberAccess in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
        {
            // Only a nested member can be null here; the lambda's own parameter is the value being validated.
            if (memberAccess.Expression is not MemberAccessExpressionSyntax conceptMember)
            {
                continue;
            }

            var conceptType = context.SemanticModel.GetTypeInfo(conceptMember, context.CancellationToken).Type;
            if (conceptType is null || !InheritsFromConceptAs(conceptType))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0013_ValidatorDereferencesNullConcept,
                memberAccess.GetLocation(),
                conceptMember.Name.Identifier.Text));
            return;
        }
    }

    static bool IsFluentValidator(INamedTypeSymbol? typeSymbol)
    {
        var current = typeSymbol;
        while (current is not null)
        {
            if (current.Name == AbstractValidatorTypeName &&
                current.ContainingNamespace?.ToDisplayString() == FluentValidationNamespace)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    static bool InheritsFromConceptAs(ITypeSymbol typeSymbol)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (current.Name == ConceptAsTypeName &&
                current.ContainingNamespace?.ToDisplayString() == ConceptsNamespace)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
