// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// The exception that is thrown when an artifact is in a named endpoint group that has no port assigned.
/// </summary>
/// <param name="group">The name of the endpoint group.</param>
/// <param name="type">The command or read model type in the group.</param>
public class EndpointGroupHasNoPort(string group, Type type)
    : Exception($"'{type.FullName}' is in endpoint group '{group}', but no port is assigned to that group. Assign one with ArcOptions.EndpointGroups.Assign(\"{group}\", <port>) or the EndpointGroups:Ports section of the Arc options. A named group is never served on the public port.");
