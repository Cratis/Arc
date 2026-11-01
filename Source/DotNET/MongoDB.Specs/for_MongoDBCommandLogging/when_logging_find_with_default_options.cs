// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_find_with_default_options : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandStarted(
        Events.Started("find", new BsonDocument { { "find", "people" }, { "filter", new BsonDocument("name", Events.Sentinel) } }));

    [Fact] void should_log_collection() => _logger.Entries[0].State["Collection"].ShouldEqual("people");
    [Fact] void should_not_log_the_filter_values() => _logger.Entries[0].Message.ShouldNotContain(Events.Sentinel);
}
