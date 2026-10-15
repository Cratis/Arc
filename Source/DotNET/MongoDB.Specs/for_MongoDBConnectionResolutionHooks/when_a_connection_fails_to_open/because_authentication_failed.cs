// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBConnectionResolutionHooks.when_a_connection_fails_to_open;

public class because_authentication_failed : given.hooks
{
    void Because()
    {
        var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var failure = new MongoConnectionException(connectionId, "Opening failed", new MongoAuthenticationException(connectionId, "Authentication failed"));
        _hooks.OnConnectionOpeningFailed(new ConnectionOpeningFailedEvent(connectionId, new ConnectionSettings(), failure, null));
    }

    [Fact] void should_drop_the_cached_resolution() => _resolver.Received(1).Invalidate(_request);
}
