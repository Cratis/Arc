// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

public sealed partial class CommandDecisionReadAnalyzer
{
    static void AnalyzeHandlerParameter(SyntaxNodeAnalysisContext context, ParameterSyntax syntax, IParameterSymbol parameter, IMethodSymbol method)
    {
        if (!IsPublicCommandHandler(method) || !IsDecision(parameter.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0039_NonIdiomaticDecisionParameter,
            syntax.GetLocation(),
            method.ContainingType.Name,
            parameter.Type.Name));
    }

    static bool IsPublicCommandHandler(IMethodSymbol method) =>
        string.Equals(method.Name, "Handle", StringComparison.Ordinal) &&
        method is { MethodKind: MethodKind.Ordinary, IsStatic: false, DeclaredAccessibility: Accessibility.Public } &&
        HasCommandAttribute(method.ContainingType);
}
