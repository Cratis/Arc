// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Roslyn lookup for the shared policy, mirroring reflection inheritance without consulting enclosing types or interfaces.
/// </summary>
internal static class CommandAttributeSymbols
{
    /// <summary>
    /// Resolves the inherited command and command-assembly declarations.
    /// </summary>
    /// <param name="commandType">The constructed command symbol.</param>
    /// <param name="hostDefault">The fallback guard mode.</param>
    /// <returns>The shared profile resolution.</returns>
    internal static (int Mode, int KeyGuard, string? Conflict) ResolveCommand(INamedTypeSymbol commandType, int hostDefault = 0) =>
        AttributePrecedencePolicy.ResolveCommand(
            Find(commandType, AttributePrecedencePolicy.ProtectedName) is not null,
            HasUnprotected(commandType),
            Value(Find(commandType, AttributePrecedencePolicy.KeyGuardName)),
            Value(Find(commandType.ContainingAssembly, AttributePrecedencePolicy.KeyGuardName)),
            hostDefault);

    /// <summary>
    /// Resolves a parameter read or an ambient method read without executing it.
    /// </summary>
    /// <param name="commandType">The command symbol.</param>
    /// <param name="readModelType">The constructed read-model symbol.</param>
    /// <param name="method">The ambient Handle or Provide method, when applicable.</param>
    /// <param name="parameter">The injected parameter, when applicable.</param>
    /// <returns>The shared read resolution.</returns>
    internal static (int Policy, bool Label, string? Conflict) ResolveRead(INamedTypeSymbol commandType, INamedTypeSymbol readModelType, IMethodSymbol? method = null, IParameterSymbol? parameter = null) =>
        AttributePrecedencePolicy.ResolveRead(ResolveCommand(commandType).KeyGuard, ReadLevels(commandType, readModelType, method, parameter));

    /// <summary>
    /// Resolves the inherited lookup declaration independently of read consistency.
    /// </summary>
    /// <param name="commandType">The command symbol.</param>
    /// <param name="parameter">The injected parameter.</param>
    /// <param name="isReadModel">Whether the parameter is a recognized read model.</param>
    /// <returns>The shared lookup resolution.</returns>
    internal static (string? Property, string? Conflict) ResolveLookup(INamedTypeSymbol commandType, IParameterSymbol parameter, bool isReadModel)
    {
        var attribute = Find(parameter, AttributePrecedencePolicy.LookUpByName);
        var property = attribute?.ConstructorArguments.FirstOrDefault().Value as string;
        return AttributePrecedencePolicy.ResolveLookup(property, isReadModel, name => HasProperty(commandType, name));
    }

    /// <summary>
    /// Tests for an inherited advisory declaration.
    /// </summary>
    /// <param name="symbol">The declaration symbol.</param>
    /// <returns>Whether Unprotected is declared.</returns>
    internal static bool HasUnprotected(ISymbol symbol) => Find(symbol, AttributePrecedencePolicy.UnprotectedName) is not null;

    /// <summary>
    /// Finds the nearest declaration along the .NET attribute inheritance chain.
    /// </summary>
    /// <param name="symbol">The constructed declaration symbol.</param>
    /// <param name="name">The attribute metadata name.</param>
    /// <returns>The nearest attribute, if any.</returns>
    internal static AttributeData? Find(ISymbol symbol, string name)
    {
        for (var current = (ISymbol?)symbol; current is not null; current = InheritedSymbol(current))
        {
            var attribute = current.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.ToDisplayString() == name);
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    static ISymbol? InheritedSymbol(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol type => type.BaseType,
        IMethodSymbol method => method.OverriddenMethod,
        IParameterSymbol { ContainingSymbol: IMethodSymbol { OverriddenMethod: { } overridden } } parameter => overridden.Parameters[parameter.Ordinal],
        _ => null
    };

    static int? Value(AttributeData? attribute) => attribute?.ConstructorArguments.FirstOrDefault().Value as int?;

    static (bool Unprotected, int? Consistency) ReadLevel(ISymbol symbol) =>
        (HasUnprotected(symbol), Value(Find(symbol, AttributePrecedencePolicy.ConsistencyName)));

    static IEnumerable<(bool Unprotected, int? Consistency)> ReadLevels(INamedTypeSymbol commandType, INamedTypeSymbol readModelType, IMethodSymbol? method, IParameterSymbol? parameter)
    {
        if (parameter is not null)
        {
            yield return ReadLevel(parameter);
        }

        if (method is not null)
        {
            yield return ReadLevel(method);
        }

        yield return ReadLevel(commandType);
        yield return ReadLevel(readModelType);
        yield return ReadLevel(commandType.ContainingAssembly);
    }

    static bool HasProperty(INamedTypeSymbol type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(name).OfType<IPropertySymbol>().Any(_ => _.DeclaredAccessibility == Accessibility.Public && !_.IsStatic))
            {
                return true;
            }
        }

        return false;
    }
}
