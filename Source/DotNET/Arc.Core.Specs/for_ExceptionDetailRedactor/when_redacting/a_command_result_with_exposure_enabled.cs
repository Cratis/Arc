// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.for_ExceptionDetailRedactor.when_redacting;

public class a_command_result_with_exposure_enabled : Specification
{
    const string OriginalMessage = "Something went wrong in a specific way";
    const string OriginalStackTrace = "   at Core.Some.Handler.Handle()";

    RecordingLogger _logger;
    CommandResult _result;

    void Establish()
    {
        _logger = new RecordingLogger();
        _result = new CommandResult
        {
            CorrelationId = CorrelationId.New(),
            ExceptionMessages = [OriginalMessage],
            ExceptionStackTrace = OriginalStackTrace
        };
    }

    void Because() => ExceptionDetailRedactor.Redact(_result, exposeExceptionDetails: true, _logger);

    [Fact] void should_keep_the_original_message() => _result.ExceptionMessages.ShouldContainOnly(OriginalMessage);
    [Fact] void should_keep_the_original_stack_trace() => _result.ExceptionStackTrace.ShouldEqual(OriginalStackTrace);
    [Fact] void should_not_log_anything() => _logger.Messages.ShouldBeEmpty();
}
