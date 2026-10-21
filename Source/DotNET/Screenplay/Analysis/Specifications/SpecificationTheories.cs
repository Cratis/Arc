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
    /// <summary>
    /// The metadata name identifying a parameterized assertion.
    /// </summary>
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

    /// <summary>
    /// Determines whether either ordinary recognition or a held scenario identifies the type.
    /// </summary>
    /// <param name="type">The scenario type.</param>
    /// <param name="recognized">Whether the ordinary reader recognizes its action.</param>
    /// <returns>Whether the type specifies scenario behavior.</returns>
    static bool IsScenario(INamedTypeSymbol type, bool recognized) => recognized || HoldsScenario(type);

    /// <summary>
    /// Determines whether the type holds a command or state-oriented scenario.
    /// </summary>
    /// <param name="type">The scenario type.</param>
    /// <returns>Whether a recognized scenario is held.</returns>
    static bool HoldsScenario(INamedTypeSymbol type) =>
        SpecificationMembers.HoldsAScenario(type) || HoldsStateScenario(type);

    /// <summary>
    /// Determines whether the type holds an event, read-model, or reactor scenario.
    /// </summary>
    /// <param name="type">The scenario type.</param>
    /// <returns>Whether a state-oriented scenario is held.</returns>
    static bool HoldsStateScenario(INamedTypeSymbol type) =>
        SpecificationMembers.HoldsAnEventScenario(type) || HoldsReadModelOrReactor(type);

    /// <summary>
    /// Determines whether the type holds a read-model or reactor scenario.
    /// </summary>
    /// <param name="type">The scenario type.</param>
    /// <returns>Whether either scenario type is held.</returns>
    static bool HoldsReadModelOrReactor(INamedTypeSymbol type) =>
        SpecificationMembers.ReadModelOf(type) is not null || SpecificationMembers.ReactorOf(type) is not null;

    /// <summary>
    /// Determines whether a theory constructs or directly executes a recognized scenario.
    /// </summary>
    /// <param name="method">The theory method.</param>
    /// <returns>Whether its body drives a scenario.</returns>
    bool DrivesScenario(IMethodSymbol method) => HandlerBodies.Of(method)
        .SelectMany(body => body.DescendantNodesAndSelf())
        .Any(node => node switch
        {
            InvocationExpressionSyntax invocation => IsScenarioCall(invocation),
            BaseObjectCreationExpressionSyntax construction => IsScenarioConstruction(construction),
            _ => false
        });

    /// <summary>
    /// Recognizes a constructed scenario by its resolved type, including target-typed constructions.
    /// </summary>
    /// <param name="construction">The source construction.</param>
    /// <returns>Whether the constructed type is a recognized scenario.</returns>
    bool IsScenarioConstruction(BaseObjectCreationExpressionSyntax construction) =>
        models.For(construction.SyntaxTree)?.GetTypeInfo(construction).Type is { } type &&
        (type.Is(WellKnownTypeNames.CommandScenario) || type.Is(WellKnownTypeNames.EventScenario) ||
        type.Is(WellKnownTypeNames.ReadModelScenario) || type.Is(WellKnownTypeNames.ReactorScenario));

    /// <summary>
    /// Recognizes a resolved scenario execution call.
    /// </summary>
    /// <param name="invocation">The source invocation.</param>
    /// <returns>Whether the call executes a scenario action.</returns>
    bool IsScenarioCall(InvocationExpressionSyntax invocation) =>
        models.For(invocation.SyntaxTree)?.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && SpecificationCalls.IsExecution(method);

    /// <summary>
    /// Reports each attributed method and any class-level theory marking.
    /// </summary>
    /// <param name="type">The scenario type.</param>
    /// <param name="theories">The attributed theory methods.</param>
    /// <param name="attributed">Whether the type itself carries theory metadata.</param>
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

    /// <summary>
    /// Reports one omitted parameterized scenario.
    /// </summary>
    /// <param name="location">The scenario or theory symbol's display name.</param>
    void Report(string location) => diagnostics.Information(
        ScreenplayDiagnosticCodes.SpecificationCasesNotFolded,
        $"The scenario '{location}' is a parameterized [Theory] and is not stated; reading its action and assertions with InlineData bindings requires a separate theory-method reader",
        location);
}
