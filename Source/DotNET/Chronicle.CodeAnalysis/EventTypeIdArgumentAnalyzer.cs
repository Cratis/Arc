// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when the [EventType] attribute specifies an explicit id argument.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeIdArgumentAnalyzer : DiagnosticAnalyzer
{
    const string EventTypeAttributeName = "Cratis.Chronicle.Events.EventTypeAttribute";
    const string IdParameterName = "id";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0004_EventTypeShouldNotSpecifyId];

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

        if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
            constructor.ContainingType?.ToDisplayString() != EventTypeAttributeName)
        {
            return;
        }

        if (!SpecifiesId(attribute.ArgumentList.Arguments))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0004_EventTypeShouldNotSpecifyId,
            attribute.GetLocation(),
            GetTargetTypeName(attribute)));
    }

    static bool SpecifiesId(SeparatedSyntaxList<AttributeArgumentSyntax> arguments) =>
        arguments.Any(argument =>
            argument.NameColon is null ||
            argument.NameColon.Name.Identifier.Text == IdParameterName);

    static string GetTargetTypeName(AttributeSyntax attribute) =>
        attribute.Parent?.Parent is BaseTypeDeclarationSyntax typeDeclaration
            ? typeDeclaration.Identifier.Text
            : "event";
}
