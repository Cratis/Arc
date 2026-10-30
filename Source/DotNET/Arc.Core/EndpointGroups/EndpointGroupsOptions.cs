// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// Represents the assignment of named endpoint groups to local ports.
/// </summary>
/// <remarks>
/// A port assigned to a group is a group port. Requests arriving on a group port reach only the groups
/// assigned to it; the default group is not served there unless <see cref="ServeDefaultGroupOnGroupPorts"/>
/// is set. Requests arriving on any other port reach only the default group. The port is the local port of the
/// connection, never the Host header, so the listener must not be reachable from outside the trusted network.
/// </remarks>
public class EndpointGroupsOptions
{
    /// <summary>
    /// Gets or sets the local ports assigned to each named group.
    /// </summary>
    public IDictionary<string, IList<int>> Ports { get; set; } = new Dictionary<string, IList<int>>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the route prefix overrides for named groups.
    /// </summary>
    public IDictionary<string, string> RoutePrefixes { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a value indicating whether the default group is also served on group ports. Defaults to false.
    /// </summary>
    public bool ServeDefaultGroupOnGroupPorts { get; set; }

    /// <summary>
    /// Gets a value indicating whether any named group has a port assigned.
    /// </summary>
    public bool HasGroups => Ports.Any(_ => _.Value.Count > 0);

    /// <summary>
    /// Sets the prefix for a named group's generated command and query routes.
    /// </summary>
    /// <param name="group">The name of the group.</param>
    /// <param name="prefix">The route prefix, without surrounding slashes.</param>
    /// <returns>The <see cref="EndpointGroupsOptions"/> for continuation.</returns>
    /// <exception cref="InvalidEndpointGroupAssignment">The group or prefix is blank.</exception>
    public EndpointGroupsOptions SetRoutePrefix(string group, string prefix)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            throw new InvalidEndpointGroupAssignment("An endpoint group must have a name.");
        }

        if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(prefix.Trim('/')))
        {
            throw new InvalidEndpointGroupAssignment($"Endpoint group '{group}' must have a non-empty route prefix.");
        }

        RoutePrefixes[group] = prefix.Trim('/');
        return this;
    }

    /// <summary>
    /// Resolves a group's route prefix, falling back to the global prefix.
    /// </summary>
    /// <param name="group">The group, or null for the default group.</param>
    /// <param name="defaultPrefix">The global route prefix.</param>
    /// <returns>The prefix for generated routes.</returns>
    public string RoutePrefixFor(string? group, string defaultPrefix) =>
        group is not null && RoutePrefixes.TryGetValue(group, out var prefix) ? prefix : defaultPrefix;

    /// <summary>
    /// Assigns one or more local ports to a named group.
    /// </summary>
    /// <param name="group">The name of the group.</param>
    /// <param name="ports">The local ports the group is served on.</param>
    /// <returns>The <see cref="EndpointGroupsOptions"/> for continuation.</returns>
    /// <exception cref="InvalidEndpointGroupAssignment">The group has no name, no port is given, or a port is out of range.</exception>
    public EndpointGroupsOptions Assign(string group, params int[] ports)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            throw new InvalidEndpointGroupAssignment("An endpoint group must have a name.");
        }

        if (ports.Length == 0)
        {
            throw new InvalidEndpointGroupAssignment($"At least one port must be assigned to endpoint group '{group}'.");
        }

        if (ports.Any(_ => _ is < 1 or > 65535))
        {
            throw new InvalidEndpointGroupAssignment($"Endpoint group '{group}' is assigned a port outside 1-65535.");
        }

        if (!Ports.TryGetValue(group, out var assigned))
        {
            assigned = [];
            Ports[group] = assigned;
        }

        foreach (var port in ports.Where(_ => !assigned.Contains(_)))
        {
            assigned.Add(port);
        }

        return this;
    }

    /// <summary>
    /// Gets the local ports assigned to a group.
    /// </summary>
    /// <param name="group">The name of the group.</param>
    /// <returns>The assigned ports.</returns>
    public IEnumerable<int> PortsFor(string group) =>
        Ports.TryGetValue(group, out var ports) ? ports : [];

    /// <summary>
    /// Checks whether a local port is assigned to any named group.
    /// </summary>
    /// <param name="port">The local port.</param>
    /// <returns>True if the port is a group port, false if not.</returns>
    public bool IsGroupPort(int port) => Ports.Any(_ => _.Value.Contains(port));

    /// <summary>
    /// Gets the <see cref="EndpointGroupScope"/> for requests arriving on a local port.
    /// </summary>
    /// <param name="port">The local port the request arrived on.</param>
    /// <returns>The groups served on that port.</returns>
    public EndpointGroupScope ScopeFor(int port)
    {
        var groups = Ports.Where(_ => _.Value.Contains(port)).Select(_ => _.Key).ToArray();
        return groups.Length == 0
            ? EndpointGroupScope.Default
            : new EndpointGroupScope(ServeDefaultGroupOnGroupPorts, groups);
    }
}
