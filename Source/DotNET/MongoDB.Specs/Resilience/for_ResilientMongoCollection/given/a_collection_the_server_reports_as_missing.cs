// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.Resilience.for_ResilientMongoCollection.given;

public class a_collection_the_server_reports_as_missing : Specification
{
    protected IMongoCollection<string> _inner;
    protected ResilientMongoCollection<string> _collection;

    void Establish()
    {
        _inner = Substitute.For<IMongoCollection<string>>();
        _collection = new(_inner, 2);
    }

    protected static MongoCommandException CommandFailure(string message)
    {
        var serverId = new ServerId(new ClusterId(0), new System.Net.DnsEndPoint("localhost", 27017));
        return new(new ConnectionId(serverId), message, new BsonDocument());
    }

    protected static MongoCommandException CollectionNotFound() => CommandFailure(WellKnownErrorMessages.CollectionNotFound);
}
