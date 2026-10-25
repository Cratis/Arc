// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// The exception that is thrown when an endpoint group is named or assigned ports in a way that cannot be served.
/// </summary>
/// <param name="reason">Why the assignment is invalid.</param>
public class InvalidEndpointGroupAssignment(string reason) : Exception(reason);
