// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_trace_is_disabled : Specification
{
    RecordingLogger _logger;
    MongoDBCommandLogging _logging;

    void Establish()
    {
        _logger = new(LogLevel.Information);
        _logging = new(_logger, false);
    }

    [Fact] void should_not_be_active() => _logging.IsActive.ShouldBeFalse();
    [Fact] void should_not_log() => _logger.Entries.ShouldBeEmpty();
}
