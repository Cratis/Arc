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
    public static bool HoldsAScenario(INamedTypeSymbol type) =>
        ChainOf(type)
            .SelectMany(_ => _.GetMembers())
            .Any(member => TypeOf(member).Is(WellKnownTypeNames.CommandScenario));

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
