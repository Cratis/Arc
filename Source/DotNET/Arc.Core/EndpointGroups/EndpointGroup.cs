// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// Resolves the endpoint group an artifact belongs to.
/// </summary>
public static class EndpointGroup
{
    static readonly ConcurrentDictionary<Type, string?> _groups = new();
    static readonly ConcurrentDictionary<MethodInfo, string?> _methodGroups = new();

    /// <summary>
    /// Gets the name of the endpoint group a type belongs to.
    /// </summary>
    /// <param name="type">The command or read model type.</param>
    /// <returns>The name of the group, or <see langword="null"/> for the default group.</returns>
    /// <remarks>
    /// An <see cref="EndpointGroupAttribute"/> on the type wins over one on the type's assembly. Custom query
    /// performers may have no type; they are in the default group.
    /// </remarks>
    public static string? For(Type type) =>
        type is null ? null : _groups.GetOrAdd(
            type,
            static _ => (_.GetCustomAttribute<EndpointGroupAttribute>(inherit: false) ??
                _.Assembly.GetCustomAttribute<EndpointGroupAttribute>())?.Name);

    /// <summary>
    /// Gets the endpoint group of a query method or controller action, falling back to its owning type.
    /// </summary>
    /// <param name="method">The query method or controller action.</param>
    /// <param name="type">The owning type.</param>
    /// <returns>The name of the group, or <see langword="null"/> for the default group.</returns>
    public static string? For(MethodInfo method, Type type) =>
        _methodGroups.GetOrAdd(method, static _ => _.GetCustomAttribute<EndpointGroupAttribute>(inherit: false)?.Name) ?? For(type);

    /// <summary>
    /// Resolves a method's endpoint group and makes sure the application serves it.
    /// </summary>
    /// <param name="method">The query method or controller action.</param>
    /// <param name="type">The owning type.</param>
    /// <param name="options">The port assignments.</param>
    /// <returns>The name of the group, or <see langword="null"/> for the default group.</returns>
    /// <exception cref="EndpointGroupHasNoPort">The method is in a named group without a port.</exception>
    public static string? EnsureServed(MethodInfo method, Type type, EndpointGroupsOptions options)
    {
        var group = For(method, type);
        if (group is not null && !options.PortsFor(group).Any())
        {
            throw new EndpointGroupHasNoPort(group, $"{type.FullName}.{method.Name}");
        }

        return group;
    }

    /// <summary>
    /// Gets the name of the endpoint group a type belongs to, and makes sure the application serves that group.
    /// </summary>
    /// <param name="type">The command or read model type.</param>
    /// <param name="options">The <see cref="EndpointGroupsOptions"/> holding the port assignments.</param>
    /// <returns>The name of the group, or <see langword="null"/> for the default group.</returns>
    /// <exception cref="EndpointGroupHasNoPort">The type is in a named group that has no port assigned.</exception>
    public static string? EnsureServed(Type type, EndpointGroupsOptions options)
    {
        var group = For(type);
        if (group is not null && !options.PortsFor(group).Any())
        {
            throw new EndpointGroupHasNoPort(group, type);
        }

        return group;
    }
}
