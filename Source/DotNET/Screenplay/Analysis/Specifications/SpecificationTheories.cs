// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reports parameterized scenarios whose theory rows cannot yet be stated.
/// </summary>
/// <param name="models">The semantic models resolving scenario calls.</param>
/// <param name="diagnostics">The diagnostics receiving omissions.</param>
internal sealed class SpecificationTheories(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    const string TheoryAttribute = "Xunit.TheoryAttribute";

    /// <summary>
    /// Reports every theory in a concrete scenario, preserving any ordinary facts.
    /// </summary>
    /// <param name="type">The possible scenario type.</param>
    /// <param name="recognized">Whether the ordinary reader recognizes its scenario action.</param>
    /// <returns>Whether the entire type is parameterized and must be skipped.</returns>
    internal bool Report(INamedTypeSymbol type, bool recognized)
    {
        if (type is not { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null })
        {
            return false;
        }

        var steps = SpecificationMembers.StepsOf(type);
        var chain = SpecificationMembers.ChainOf(type).Concat(SpecificationMembers.ChainOf(steps)).Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>().ToList();
        var theories = chain.SelectMany(member => member.GetMembers()).OfType<IMethodSymbol>().Where(method => method.HasAttribute(TheoryAttribute)).ToList();
        var attributed = chain.Exists(member => member.HasAttribute(TheoryAttribute));
        if (!attributed && theories.Count == 0)
        {
            return false;
        }

        if (!IsScenario(steps, recognized) && !theories.Exists(DrivesScenario))
        {
            return false;
        }

        Report(type, theories, attributed);
        return attributed || !SpecificationMembers.AssertionsIn(type).Any();
    }

    static bool IsScenario(INamedTypeSymbol type, bool recognized) => recognized || HoldsScenario(type);

    static bool HoldsScenario(INamedTypeSymbol type) =>
        SpecificationMembers.HoldsAScenario(type) || HoldsStateScenario(type);

    static bool HoldsStateScenario(INamedTypeSymbol type) =>
        SpecificationMembers.HoldsAnEventScenario(type) || SpecificationMembers.ReadModelOf(type) is not null;

    bool DrivesScenario(IMethodSymbol method) => HandlerBodies.Of(method)
        .SelectMany(body => body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        .Any(IsScenarioCall);

    bool IsScenarioCall(InvocationExpressionSyntax invocation) =>
        models.For(invocation.SyntaxTree)?.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && SpecificationCalls.IsExecution(method);

    void Report(INamedTypeSymbol type, IEnumerable<IMethodSymbol> theories, bool attributed)
    {
        foreach (var theory in theories)
        {
            Report(theory.ToDisplayString());
        }

        if (attributed)
        {
            Report(type.ToDisplayString());
        }
    }

    void Report(string location) => diagnostics.Information(
        ScreenplayDiagnosticCodes.SpecificationCasesNotFolded,
        $"The scenario '{location}' is a parameterised [Theory] and is not stated; case tables are not generated yet",
        location);
}
