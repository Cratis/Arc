// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.for_ExceptionDetailRedactor.when_redacting;

public class a_command_result_with_exposure_disabled : Specification
{
    const string SecretMessage = "Object reference not set to an instance of an object at SecretType.SecretMethod";
    const string SecretStackTrace = "   at Core.Secret.Handler.Handle() in /src/Secret.cs:line 42";

    CorrelationId _correlationId;
    RecordingLogger _logger;
    CommandResult _result;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _logger = new RecordingLogger();
        _result = new CommandResult
        {
            CorrelationId = _correlationId,
            ExceptionMessages = [SecretMessage],
            ExceptionStackTrace = SecretStackTrace
        };
    }

    void Because() => ExceptionDetailRedactor.Redact(_result, exposeExceptionDetails: false, _logger);

    [Fact] void should_still_report_having_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_retain_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_replace_the_messages_with_a_generic_marker() => _result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
    [Fact] void should_not_leak_the_original_message() => _result.ExceptionMessages.ShouldNotContain(SecretMessage);
    [Fact] void should_clear_the_stack_trace() => _result.ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_log_the_full_message_server_side() => _logger.Messages.Exists(message => message.Contains(SecretMessage)).ShouldBeTrue();
    [Fact] void should_log_the_full_stack_trace_server_side() => _logger.Messages.Exists(message => message.Contains(SecretStackTrace)).ShouldBeTrue();
}
