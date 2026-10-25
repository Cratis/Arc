// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups;

/// <summary>
/// The exception that is thrown when every port the server listens on is assigned to a named endpoint group,
/// which would make a group port the public port.
/// </summary>
/// <param name="ports">The ports the server listens on.</param>
public class NoPublicListenerForDefaultEndpointGroup(IEnumerable<int> ports)
    : Exception($"Every port the server listens on ({string.Join(", ", ports)}) is assigned to a named endpoint group, so a group port is also the public port and the default endpoints have none. Give each endpoint group its own listener, separate from the public one.");
