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
    /// <returns>The shared profile resolution.</returns>
    internal static (int Mode, int ReadProtection, string? Conflict) ResolveCommand(Type commandType)
    {
        var attributes = commandType.GetCustomAttributes(true);
        return AttributePrecedencePolicy.ResolveCommand(
            attributes.Any(_ => _ is IProtectedDecisionAttribute),
            attributes.Any(_ => _ is IUnprotectedDecisionAttribute),
            ReadProtection(attributes),
            ReadProtection(commandType.Assembly.GetCustomAttributes()));
    }

    /// <summary>
    /// Records parameter advisory status and method-body declarations without executing reads.
    /// </summary>
    /// <param name="readModelType">The closed read-model type.</param>
    /// <param name="method">The ambient Handle or Provide method, when applicable.</param>
    /// <param name="parameter">The injected parameter, when applicable.</param>
    /// <returns>The shared read declarations.</returns>
    internal static (bool Advisory, bool MethodBodyUnprotected) ResolveRead(Type readModelType, MethodInfo? method = null, ParameterInfo? parameter = null) =>
        AttributePrecedencePolicy.ResolveRead(
            parameter is not null && HasUnprotected(Attribute.GetCustomAttributes(parameter, true)),
            HasUnprotected(readModelType.GetCustomAttributes(true)),
            method is not null && HasUnprotected(Attribute.GetCustomAttributes(method, true)));

    /// <summary>
    /// Resolves the inherited lookup declaration in every read-protection mode.
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
    /// <exception cref="CommandCannotBeBothProtectedAndUnprotected">Both protection declarations were found.</exception>
    /// <exception cref="ConflictingReadProtection">An unprotected command explicitly enables read protection.</exception>
    internal static void ThrowIfConflicting(Type commandType, string? conflict)
    {
        if (conflict == AttributePrecedencePolicy.ProtectionConflict)
        {
            throw new CommandCannotBeBothProtectedAndUnprotected(commandType);
        }

        if (conflict == AttributePrecedencePolicy.ReadProtectionConflict)
        {
            throw new ConflictingReadProtection(commandType);
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

    static int? ReadProtection(IEnumerable<object> attributes) => attributes.OfType<ReadProtectionAttribute>().Select(_ => (int?)_.Mode).FirstOrDefault();

    static bool HasUnprotected(IEnumerable<object> attributes) => attributes.Any(_ => _ is IUnprotectedDecisionAttribute);
}
