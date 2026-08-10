// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Finds the parts of a specification within the chain of types it is written across.
/// </summary>
/// <remarks>
/// A specification inherits the world it starts from. Every type in the chain may set part of it up, and the
/// specification framework calls each of those in turn from the base down, so the chain is walked in that same order
/// and what it states comes out in the order it really happens in.
/// <para>
/// Where the steps are written differs between the two shapes Arc documents. A specification driving the pipeline in
/// process writes them on itself; one driving a running host writes them on a nested type the fixture is handed to,
/// and keeps only its assertions outside. Which of the two a specification is follows from where the steps are, not
/// from what it derives from, so a chain no package declares reads exactly the same way.
/// </para>
/// </remarks>
public static class SpecificationMembers
{
    /// <summary>The method setting up the world a specification starts from.</summary>
    public const string EstablishMethod = "Establish";

    /// <summary>The method performing the single action a specification is about.</summary>
    public const string BecauseMethod = "Because";

    /// <summary>The nested type the steps of a specification against a running host are written on.</summary>
    public const string ContextType = "context";

    /// <summary>
    /// Gets the type the steps of a specification are written on.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <returns>The nested context when the specification has one, otherwise the type itself.</returns>
    public static INamedTypeSymbol StepsOf(INamedTypeSymbol type) =>
        type.GetTypeMembers(ContextType).FirstOrDefault(_ => Declares(_, BecauseMethod) || Declares(_, EstablishMethod)) ?? type;

    /// <summary>
    /// Gets the chain of types a type is written across, from the base down.
    /// </summary>
    /// <param name="type">The type to walk.</param>
    /// <returns>The chain, base first.</returns>
    public static IEnumerable<INamedTypeSymbol> ChainOf(INamedTypeSymbol type)
    {
        var chain = new List<INamedTypeSymbol>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            chain.Insert(0, current);
        }

        return chain;
    }

    /// <summary>
    /// Gets every declaration of a method in a chain of types.
    /// </summary>
    /// <param name="type">The type to walk from.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns>The methods, from the base down.</returns>
    public static IEnumerable<IMethodSymbol> MethodsIn(INamedTypeSymbol type, string name) =>
        ChainOf(type).SelectMany(_ => _.GetMembers(name)).OfType<IMethodSymbol>().Where(_ => _.MethodKind == MethodKind.Ordinary);

    /// <summary>
    /// Gets every assertion a specification makes.
    /// </summary>
    /// <param name="type">The type declaring the specification.</param>
    /// <returns>The assertions, ordered by name so that the same source always reads the same way.</returns>
    public static IEnumerable<IMethodSymbol> AssertionsIn(INamedTypeSymbol type) =>
        ChainOf(type)
            .SelectMany(_ => _.GetMembers())
            .OfType<IMethodSymbol>()
            .Where(_ => _.HasAttribute(WellKnownTypeNames.FactAttribute))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ThenBy(_ => _.ToDisplayString(), StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a type holds a scenario the command pipeline is driven through.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type or one of its bases holds a scenario.</returns>
    /// <remarks>
    /// Holding one is what makes a specification an integration specification of the slice rather than a unit level
    /// one about a collaborator, and it holds whether or not the command is issued somewhere this can read. That is
    /// exactly why it is asked: a specification that holds a scenario and issues its command through a helper is one
    /// this cannot read, and saying so is the difference between a known gap and a silent one.
    /// </remarks>
    public static bool HoldsAScenario(INamedTypeSymbol type) => Holds(type, WellKnownTypeNames.CommandScenario) is not null;

    /// <summary>
    /// Gets the read model a type is written as a scenario of.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The read model, or <see langword="null"/> when the type holds no read model scenario.</returns>
    /// <remarks>
    /// A read model scenario says which read model it is about in its own type argument, which is the whole of what
    /// the outcome of such a specification is: the events went in and this is what they built. Nothing in the body
    /// has to be read to know it, so a scenario whose assertions are written in a way this cannot follow still says
    /// exactly as much as the language holds for it.
    /// </remarks>
    public static ITypeSymbol? ReadModelOf(INamedTypeSymbol type) =>
        Holds(type, WellKnownTypeNames.ReadModelScenario) is INamedTypeSymbol { TypeArguments.Length: 1 } scenario
            ? scenario.TypeArguments[0]
            : null;

    /// <summary>
    /// Gets the scenario a type holds of a kind Screenplay has no way to read.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The name of the scenario, or <see langword="null"/> when the type holds none of them.</returns>
    /// <remarks>
    /// A scenario is what tells a specification of a slice apart from a unit level one about a collaborator, so a
    /// type holding one is specifying the slice whether or not this can express what it says. Which of them it holds
    /// is what the report needs: the gap between an append that is rejected and a reactor asked what it did is not
    /// one gap, and a reader cannot act on being told only that something was left out.
    /// </remarks>
    public static string? ScenarioWithoutCounterpart(INamedTypeSymbol type) =>
        Holds(type, WellKnownTypeNames.EventScenario)?.Name ??
        Holds(type, WellKnownTypeNames.ReactorScenario)?.Name;

    /// <summary>
    /// Gets the scenario of a kind a type holds.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the scenario.</param>
    /// <returns>The type of the scenario held, or <see langword="null"/> when the type holds none.</returns>
    static ITypeSymbol? Holds(INamedTypeSymbol type, string fullMetadataName) =>
        ChainOf(type)
            .SelectMany(_ => _.GetMembers())
            .Select(TypeOf)
            .FirstOrDefault(held => held.Is(fullMetadataName));

    /// <summary>
    /// Determines whether a type declares a method itself.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns>True when the type declares it.</returns>
    static bool Declares(INamedTypeSymbol type, string name) => type.GetMembers(name).OfType<IMethodSymbol>().Any();

    /// <summary>
    /// Gets the type a member holds a value of.
    /// </summary>
    /// <param name="member">The member to read.</param>
    /// <returns>The type, or <see langword="null"/> when the member holds no value.</returns>
    static ITypeSymbol? TypeOf(ISymbol member) => member switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => null
    };
}
