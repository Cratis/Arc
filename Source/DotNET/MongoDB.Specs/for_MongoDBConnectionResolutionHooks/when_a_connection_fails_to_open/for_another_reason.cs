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

public class for_another_reason : given.hooks
{
    void Because()
    {
        var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        _hooks.OnConnectionOpeningFailed(new ConnectionOpeningFailedEvent(connectionId, new ConnectionSettings(), new MongoConnectionException(connectionId, "Refused"), null));
    }

    [Fact] void should_keep_the_cached_resolution() => _resolver.DidNotReceive().Invalidate(Arg.Any<Cratis.Arc.Connections.ConnectionRequest>());
}
