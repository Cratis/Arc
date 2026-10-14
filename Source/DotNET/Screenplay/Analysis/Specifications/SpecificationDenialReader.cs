// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads a denial only when every assertion restates that denial on the action's own result.
/// </summary>
/// <param name="models">The semantic models proving the result and scenario identities.</param>
/// <param name="diagnostics">The diagnostics used by the shared result evidence reader.</param>
internal sealed class SpecificationDenialReader(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads a denial before any event or validation assertions can be discarded.
    /// </summary>
    /// <param name="bodies">The bound assertion bodies.</param>
    /// <param name="scenario">The scenario containing the assertions.</param>
    /// <param name="action">The command action, when recovered.</param>
    /// <param name="draft">The scenario being read.</param>
    /// <returns>Whether the scenario attempts to assert a denial.</returns>
    internal bool Read(IEnumerable<(SyntaxNode Body, SemanticModel Model)> bodies, INamedTypeSymbol scenario, InvocationExpressionSyntax? action, SpecificationDraft draft)
    {
        var calls = bodies.SelectMany(item => item.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .Select(call => (Call: call, item.Body, item.Model, Method: item.Model.GetSymbolInfo(call).Symbol as IMethodSymbol))).ToList();
        if (!calls.Exists(item => item.Method?.Name == "ShouldNotBeAuthorized"))
        {
            return false;
        }

        var resultEvidence = new SpecificationReturnValues(new MappingSourceReader(diagnostics), models);
        foreach (var (call, body, model, method) in calls)
        {
            var reason = method is not null
                ? AssertionFailure(call, body, model, method, scenario, action, resultEvidence)
                : "its denial is combined with another outcome or an assertion that cannot be proven redundant";
            if (reason is not null)
            {
                draft.CannotRead(reason);
                return true;
            }
        }

        draft.Denied = true;
        return true;
    }

    static string? AssertionFailure(
        InvocationExpressionSyntax call,
        SyntaxNode body,
        SemanticModel model,
        IMethodSymbol method,
        INamedTypeSymbol scenario,
        InvocationExpressionSyntax? action,
        SpecificationReturnValues evidence)
    {
        var denial = method.Name == "ShouldNotBeAuthorized";
        if (!(denial ? SpecificationAssertions.IsDenial(method) : SpecificationAssertions.IsRedundantDenialAssertion(method)))
        {
            return denial
                ? "its 'ShouldNotBeAuthorized' denial assertion does not match the exact command-result testing API"
                : "its denial is combined with another outcome or an assertion that cannot be proven redundant";
        }
        if (!StepsTaken.AlwaysCompletesThrough(call, body))
        {
            return "its denial assertion is conditional or may not complete";
        }

        var subject = method.ReducedFrom is not null && call.Expression is MemberAccessExpressionSyntax member
            ? member.Expression : call.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        return action is not null && subject is not null && evidence.IsActionResult(subject, model, scenario, action)
            ? null : "its denial is not asserted on the stable result of its command action";
    }
}
