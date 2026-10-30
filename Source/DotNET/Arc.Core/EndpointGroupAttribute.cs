// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Places a command or a read model's queries in a named endpoint group, which is reachable only through the
/// local ports the application assigns to that group.
/// </summary>
/// <remarks>
/// Put it on a command type, a read model type, an assembly, a query method or a controller action.
/// A method attribute wins over its type, and a type attribute wins over its assembly. On any other method
/// it has no effect. Artifacts without the attribute stay in the default group and are served exactly as before.
/// A named group must be assigned at least one port through <see cref="EndpointGroups.EndpointGroupsOptions"/>,
/// otherwise the application refuses to start.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class EndpointGroupAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointGroupAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the endpoint group.</param>
    /// <exception cref="EndpointGroups.InvalidEndpointGroupAssignment">The name is empty.</exception>
    public EndpointGroupAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new EndpointGroups.InvalidEndpointGroupAssignment("An endpoint group must have a name.");
        }

        Name = name;
    }

    /// <summary>
    /// Gets the name of the endpoint group.
    /// </summary>
    public string Name { get; }
}
