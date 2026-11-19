// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_creating_a_missing_handler_result : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    CommandResult _result;

    void Because() => _result = CommandResult.MissingHandler(_correlationId, typeof(string));

    [Fact] void should_identify_the_missing_handler() => _result.IsMissingHandler.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_still_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_still_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_preserve_the_message() => _result.ExceptionMessages.ShouldContain("No handler found for command of type System.String");
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_be_retryable() => _result.IsRetryable.ShouldBeFalse();
}
