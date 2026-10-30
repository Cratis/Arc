// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// Resolves endpoint groups for query performers without changing their contract.
/// </summary>
public static class QueryPerformerEndpointGroupExtensions
{
    /// <summary>
    /// Gets a query's method-level group, falling back to its type and assembly.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <returns>The group name, or null for the default group.</returns>
    /// <exception cref="AmbiguousQueryEndpointGroup">Query overloads resolve to different groups.</exception>
    public static string? GetEndpointGroup(this IQueryPerformer performer)
    {
        if (performer.Type is not { } type)
        {
            return null;
        }

        if (performer is IAuthorizationQueryTarget known)
        {
            return EndpointGroup.For(known.AuthorizationMethod, type);
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == performer.Name.Value).ToArray();
        if (methods.Length == 0)
        {
            return EndpointGroup.For(type);
        }

        var groups = methods.Select(method => EndpointGroup.For(method, type)).Distinct().ToArray();
        if (groups.Length > 1)
        {
            throw new AmbiguousQueryEndpointGroup(performer.FullyQualifiedName);
        }

        return groups[0];
    }

    /// <summary>
    /// Resolves the query's group and makes sure a port is assigned to it.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <param name="options">The port assignments.</param>
    /// <returns>The group name, or null for the default group.</returns>
    /// <exception cref="EndpointGroupHasNoPort">The query's group has no port.</exception>
    public static string? EnsureEndpointGroupServed(this IQueryPerformer performer, EndpointGroupsOptions options)
    {
        var group = performer.GetEndpointGroup();
        if (group is not null && !options.PortsFor(group).Any())
        {
            var methodName = performer is IAuthorizationQueryTarget known ? known.AuthorizationMethod.Name : performer.Name.Value;
            throw new EndpointGroupHasNoPort(group, $"{performer.Type.FullName}.{methodName}");
        }

        return group;
    }
}
