// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_creating_other_results : Specification
{
    [Fact] void should_not_mark_success_as_missing_a_handler() => CommandResult.Success(CorrelationId.New()).IsMissingHandler.ShouldBeFalse();
    [Fact] void should_not_mark_errors_as_missing_a_handler() => CommandResult.Error(CorrelationId.New(), "failed").IsMissingHandler.ShouldBeFalse();
    [Fact] void should_not_mark_retries_as_missing_a_handler() => CommandResult.Retryable(CorrelationId.New(), "retry").IsMissingHandler.ShouldBeFalse();
    [Fact] void should_not_mark_denials_as_missing_a_handler() => CommandResult.Unauthorized(CorrelationId.New()).IsMissingHandler.ShouldBeFalse();
    [Fact] void should_not_mark_typed_success_as_missing_a_handler() => CommandResult<string>.Success(CorrelationId.New()).IsMissingHandler.ShouldBeFalse();
}
