// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_update_with_default_options : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandStarted(
        Events.Started("update", new BsonDocument { { "update", "people" }, { "updates", new BsonArray { new BsonDocument { { "q", new BsonDocument("name", Events.Sentinel) }, { "u", new BsonDocument("$set", new BsonDocument("name", Events.Sentinel)) } } } } }));

    [Fact] void should_log_collection() => _logger.Entries[0].State["Collection"].ShouldEqual("people");
    [Fact] void should_not_log_the_filter_or_update_values() => _logger.Entries[0].Message.ShouldNotContain(Events.Sentinel);
}
