// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public static class Events
{
    public const string Sentinel = "SENTINEL-personal-value";

    static ConnectionId ConnectionId => new(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 1);

    public static CommandStartedEvent Started(string commandName, BsonDocument command) =>
        new(commandName, command, new DatabaseNamespace("people-db"), null, 42, ConnectionId);

    public static CommandSucceededEvent Succeeded(string commandName) =>
        new(commandName, new BsonDocument("ok", 1), new DatabaseNamespace("people-db"), null, 42, ConnectionId, TimeSpan.FromMilliseconds(12));

    public static CommandFailedEvent Failed(string commandName, Exception failure) =>
        new(commandName, new DatabaseNamespace("people-db"), failure, null, 42, ConnectionId, TimeSpan.FromMilliseconds(12));

    public static MongoCommandException CommandException() =>
        new(ConnectionId, $"E11000 duplicate key error dup key: {{ name: \"{Sentinel}\" }}", new BsonDocument(), new BsonDocument { { "code", 11000 }, { "codeName", "DuplicateKey" } });
}
