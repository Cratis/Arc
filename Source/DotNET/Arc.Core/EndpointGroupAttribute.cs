// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Places a command or a read model's queries in a named endpoint group, which is reachable only through the
/// local ports the application assigns to that group.
/// </summary>
/// <remarks>
/// Put it on a command type, a read model type, or an assembly. An attribute on the type wins over one on its
/// assembly. Artifacts without the attribute stay in the default group and are served exactly as before.
/// A named group must be assigned at least one port through <see cref="EndpointGroups.EndpointGroupsOptions"/>,
/// otherwise the application refuses to start.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class EndpointGroupAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointGroupAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the endpoint group.</param>
    public EndpointGroupAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the name of the endpoint group.
    /// </summary>
    public string Name { get; }
}
