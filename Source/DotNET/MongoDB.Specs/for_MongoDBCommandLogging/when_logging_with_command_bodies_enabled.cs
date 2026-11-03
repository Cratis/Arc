// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_with_command_bodies_enabled : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because()
    {
        var logging = new MongoDBCommandLogging(_logger, true);
        logging.CommandStarted(Events.Started("insert", new BsonDocument { { "insert", "people" }, { "documents", new BsonArray { new BsonDocument("name", Events.Sentinel) } } }));
        logging.CommandFailed(Events.Failed("insert", Events.CommandException()));
    }

    [Fact] void should_log_the_command_body() => _logger.Entries[0].Message.ShouldContain(Events.Sentinel);
    [Fact] void should_log_the_failure_message() => _logger.Entries[1].Message.ShouldContain(Events.Sentinel);
}
