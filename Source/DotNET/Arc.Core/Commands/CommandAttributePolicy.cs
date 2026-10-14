// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Reflection lookup for the shared declaration policy. Resolving a declaration performs no reads or enrollment.
/// </summary>
internal static class CommandAttributePolicy
{
    /// <summary>
    /// Resolves the inherited command profile and its assembly rollout declaration.
    /// </summary>
    /// <param name="commandType">The closed command type.</param>
    /// <param name="hostDefault">The fallback guard mode.</param>
    /// <returns>The shared profile resolution.</returns>
    internal static (int Mode, int KeyGuard, string? Conflict) ResolveCommand(Type commandType, KeyGuardMode hostDefault = KeyGuardMode.Off)
    {
        var attributes = commandType.GetCustomAttributes(true);
        return AttributePrecedencePolicy.ResolveCommand(
            attributes.Any(_ => _ is IProtectedDecisionAttribute),
            attributes.Any(_ => _ is IUnprotectedDecisionAttribute),
            KeyGuard(attributes),
            KeyGuard(commandType.Assembly.GetCustomAttributes()),
            (int)hostDefault);
    }

    /// <summary>
    /// Resolves a parameter read or an ambient method read without executing it.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="readModelType">The closed read-model type.</param>
    /// <param name="method">The ambient Handle or Provide method, when applicable.</param>
    /// <param name="parameter">The injected parameter, when applicable.</param>
    /// <returns>The shared read resolution.</returns>
    internal static (int Policy, bool Label, string? Conflict) ResolveRead(Type commandType, Type readModelType, MethodInfo? method = null, ParameterInfo? parameter = null) =>
        AttributePrecedencePolicy.ResolveRead(ResolveCommand(commandType).KeyGuard, ReadLevels(commandType, readModelType, method, parameter));

    /// <summary>
    /// Resolves the inherited lookup declaration in every guard mode.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="parameter">The injected parameter.</param>
    /// <param name="isReadModel">Whether the parameter is a recognized read model.</param>
    /// <returns>The shared lookup resolution.</returns>
    internal static (string? Property, string? Conflict) ResolveLookup(Type commandType, ParameterInfo parameter, bool isReadModel)
    {
        var property = Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().FirstOrDefault()?.Property;
        return AttributePrecedencePolicy.ResolveLookup(property, isReadModel, name => commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(_ => _.Name == name));
    }

    /// <summary>
    /// Reports a profile conflict at the pipeline's existing guarded conflict boundary.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="conflict">The resolved conflict.</param>
    /// <param name="mode">The resolved decision mode.</param>
    /// <exception cref="CommandCannotBeBothProtectedAndUnprotected">Both protection declarations were found.</exception>
    /// <exception cref="CommandCannotCombineProtectedDecisionAndKeyGuard">A protection declaration explicitly enables the key guard.</exception>
    internal static void ThrowIfConflicting(Type commandType, string? conflict, CommandDecisionMode mode)
    {
        if (conflict == AttributePrecedencePolicy.ProtectionConflict)
        {
            throw new CommandCannotBeBothProtectedAndUnprotected(commandType);
        }

        if (conflict == AttributePrecedencePolicy.KeyGuardConflict)
        {
            throw new CommandCannotCombineProtectedDecisionAndKeyGuard(commandType, mode == CommandDecisionMode.Protected ? "protected decision" : "unprotected");
        }
    }

    /// <summary>
    /// Reports a conflicting read declaration without performing a read.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="conflict">The resolved conflict.</param>
    /// <exception cref="ConflictingReadPolicy">Both read declarations were found at one level.</exception>
    internal static void ThrowIfInvalidRead(Type commandType, string? conflict)
    {
        if (conflict == AttributePrecedencePolicy.ReadConflict)
        {
            throw new ConflictingReadPolicy(commandType);
        }
    }

    /// <summary>
    /// Reports an invalid explicit lookup without changing argument resolution.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="property">The declared property name.</param>
    /// <param name="conflict">The resolved conflict.</param>
    /// <exception cref="InvalidLookUpBy">The property is missing or the parameter is not a read model.</exception>
    internal static void ThrowIfInvalidLookup(Type commandType, string property, string? conflict)
    {
        if (conflict == AttributePrecedencePolicy.LookupConflict)
        {
            throw new InvalidLookUpBy(commandType, property);
        }
    }

    static int? KeyGuard(IEnumerable<object> attributes) => attributes.OfType<IKeyGuardAttribute>().Select(_ => (int?)_.Mode).FirstOrDefault();

    static (bool Unprotected, int? Consistency) ReadLevel(IEnumerable<object> attributes)
    {
        var declarations = attributes.ToArray();
        return (declarations.Any(_ => _ is IUnprotectedDecisionAttribute), declarations.OfType<ConsistencyAttribute>().Select(_ => (int?)_.Consistency).FirstOrDefault());
    }

    static IEnumerable<(bool Unprotected, int? Consistency)> ReadLevels(Type commandType, Type readModelType, MethodInfo? method, ParameterInfo? parameter)
    {
        if (parameter is not null)
        {
            yield return ReadLevel(Attribute.GetCustomAttributes(parameter, true));
        }

        if (method is not null)
        {
            yield return ReadLevel(Attribute.GetCustomAttributes(method, true));
        }

        yield return ReadLevel(commandType.GetCustomAttributes(true));
        yield return ReadLevel(readModelType.GetCustomAttributes(true));
        yield return ReadLevel(commandType.Assembly.GetCustomAttributes());
    }
}
