// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_success : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandSucceeded(Events.Succeeded("insert"));

    [Fact] void should_log_request_id() => _logger.Entries[0].State["RequestId"].ShouldEqual(42);
    [Fact] void should_log_command_name() => _logger.Entries[0].State["CommandName"].ShouldEqual("insert");
    [Fact] void should_log_duration() => _logger.Entries[0].State["DurationMs"].ShouldEqual(12d);
}
