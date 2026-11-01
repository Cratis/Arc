// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_users_info_with_default_options : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandStarted(
        Events.Started("usersInfo", new BsonDocument { { "usersInfo", "alice" } }));

    [Fact] void should_log_no_collection() => _logger.Entries[0].State["Collection"].ShouldBeNull();
}
