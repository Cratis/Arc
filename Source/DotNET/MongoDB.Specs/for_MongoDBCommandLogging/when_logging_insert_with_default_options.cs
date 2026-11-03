// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_insert_with_default_options : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandStarted(
        Events.Started("insert", new BsonDocument { { "insert", "people" }, { "documents", new BsonArray { new BsonDocument("name", Events.Sentinel) } } }));

    [Fact] void should_log_request_id() => _logger.Entries[0].State["RequestId"].ShouldEqual(42);
    [Fact] void should_log_command_name() => _logger.Entries[0].State["CommandName"].ShouldEqual("insert");
    [Fact] void should_log_database() => _logger.Entries[0].State["Database"].ShouldEqual("people-db");
    [Fact] void should_log_collection() => _logger.Entries[0].State["Collection"].ShouldEqual("people");
    [Fact] void should_not_log_the_values() => _logger.Entries[0].Message.ShouldNotContain(Events.Sentinel);
    [Fact] void should_not_carry_the_values_in_state() => _logger.Entries[0].State.Values.Any(_ => _?.ToString()?.Contains(Events.Sentinel) == true).ShouldBeFalse();
}
