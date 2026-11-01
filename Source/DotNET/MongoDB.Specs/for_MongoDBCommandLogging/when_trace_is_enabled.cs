// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.MongoDB.for_MongoDBCommandLogging;

public class when_trace_is_enabled : Specification
{
    [Fact] void should_be_active() => new MongoDBCommandLogging(new RecordingLogger(LogLevel.Trace), false).IsActive.ShouldBeTrue();
}
