// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Commands;

/// <summary>
/// Roslyn lookup for the shared policy, mirroring reflection inheritance without consulting enclosing types or interfaces.
/// </summary>
internal static class CommandAttributeSymbols
{
    /// <summary>
    /// Resolves the inherited command and command-assembly declarations.
    /// </summary>
    /// <param name="commandType">The constructed command symbol.</param>
    /// <returns>The shared profile resolution.</returns>
    internal static (int Mode, int ReadProtection, string? Conflict) ResolveCommand(INamedTypeSymbol commandType) =>
        AttributePrecedencePolicy.ResolveCommand(
            Find(commandType, AttributePrecedencePolicy.ProtectedName) is not null,
            HasUnprotected(commandType),
            Value(Find(commandType, AttributePrecedencePolicy.ReadProtectionName)),
            Value(Find(commandType.ContainingAssembly, AttributePrecedencePolicy.ReadProtectionName)));

    /// <summary>
    /// Records parameter advisory status and method-body declarations without executing reads.
    /// </summary>
    /// <param name="readModelType">The constructed read-model symbol.</param>
    /// <param name="method">The ambient Handle or Provide method, when applicable.</param>
    /// <param name="parameter">The injected parameter, when applicable.</param>
    /// <returns>The shared read declarations.</returns>
    internal static (bool Advisory, bool MethodBodyUnprotected) ResolveRead(INamedTypeSymbol readModelType, IMethodSymbol? method = null, IParameterSymbol? parameter = null) =>
        AttributePrecedencePolicy.ResolveRead(
            parameter is not null && HasUnprotected(parameter),
            HasUnprotected(readModelType),
            method is not null && HasUnprotected(method));

    /// <summary>
    /// Resolves the inherited lookup declaration independently of read protection.
    /// </summary>
    /// <param name="commandType">The command symbol.</param>
    /// <param name="parameter">The injected parameter.</param>
    /// <param name="isReadModel">Whether the parameter is a recognized read model.</param>
    /// <returns>The shared lookup resolution.</returns>
    internal static (string? Property, string? Conflict) ResolveLookup(INamedTypeSymbol commandType, IParameterSymbol parameter, bool isReadModel)
    {
        var attribute = Find(parameter, AttributePrecedencePolicy.LookUpByName);
        var property = attribute?.ConstructorArguments.FirstOrDefault().Value as string;
        return AttributePrecedencePolicy.ResolveLookup(property, isReadModel, name => PublicProperties(commandType).Any(_ => _.Name == name));
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

    /// <summary>
    /// Finds public instance properties, including inherited declarations.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns>The lookup candidate properties.</returns>
    internal static IEnumerable<IPropertySymbol> PublicProperties(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>().Where(_ => _.DeclaredAccessibility == Accessibility.Public && !_.IsStatic && !_.IsIndexer))
            {
                yield return property;
            }
        }
    }

    static ISymbol? InheritedSymbol(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol type => type.BaseType,
        IMethodSymbol method => method.OverriddenMethod,
        IParameterSymbol { ContainingSymbol: IMethodSymbol { OverriddenMethod: { } overridden } } parameter => overridden.Parameters[parameter.Ordinal],
        _ => null
    };

    static int? Value(AttributeData? attribute) => attribute?.ConstructorArguments.FirstOrDefault().Value as int?;
}
