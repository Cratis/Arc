// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_logging_failure_with_default_options : Specification
{
    RecordingLogger _logger;

    void Establish() => _logger = new(LogLevel.Trace);

    void Because() => new MongoDBCommandLogging(_logger, false).CommandFailed(Events.Failed("insert", Events.CommandException()));

    [Fact] void should_not_log_the_failure_message() => _logger.Entries[0].Message.ShouldNotContain(Events.Sentinel);
    [Fact] void should_log_the_failure_type() => _logger.Entries[0].State["FailureType"].ShouldEqual(nameof(MongoCommandException));
    [Fact] void should_log_the_code() => _logger.Entries[0].State["Code"].ShouldEqual(11000);
    [Fact] void should_log_the_code_name() => _logger.Entries[0].State["CodeName"].ShouldEqual("DuplicateKey");
}
