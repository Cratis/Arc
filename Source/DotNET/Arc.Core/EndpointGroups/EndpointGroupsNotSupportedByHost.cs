// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// The exception that is thrown when the host cannot enforce endpoint group ports.
/// </summary>
/// <param name="host">A description of the host.</param>
public class EndpointGroupsNotSupportedByHost(string host)
    : Exception($"Named endpoint groups are not supported by {host}, because it cannot restrict an endpoint to a local port. Host the application on ASP.NET Core, or remove the EndpointGroup attributes and port assignments.");
