// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBChangeStreamSupport;

public class when_classifying_driver_failures : Specification
{
    bool[] _unsupported;
    bool[] _otherFailures;

    void Because()
    {
        _unsupported = new[]
        {
            Error(40573, "The $changeStream stage is only supported on replica sets"),
            Error(40324, "Unrecognized pipeline stage name: '$changeStream'"),
            Error(16436, "Unrecognized pipeline stage name: '$changeStream'"),
            Error(115, "$changeStream is not supported"),
            Error(0, "$changeStream is not supported")
        }.Select(MongoDBChangeStreamSupport.IsMissing).ToArray();
        _otherFailures = new[]
        {
            Error(91, "Primary stepped down"),
            Error(13, "Unauthorized to use $changeStream"),
            Error(286, "$changeStream history lost"),
            Error(40324, "Unrecognized pipeline stage name: '$anotherStage'"),
            Error(115, "Another command is not supported")
        }.Select(MongoDBChangeStreamSupport.IsMissing).ToArray();
    }

    [Fact] void should_recognize_missing_change_streams() => _unsupported.ShouldEachConformTo(value => value);
    [Fact] void should_not_cache_unrelated_failures_as_missing_support() => _otherFailures.ShouldEachConformTo(value => !value);

    internal static MongoCommandException Error(int code, string message) => new(
        new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
        message,
        new BsonDocument(),
        new BsonDocument("code", code));
}
