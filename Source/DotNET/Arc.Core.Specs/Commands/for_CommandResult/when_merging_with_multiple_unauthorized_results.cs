// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_merging_with_multiple_unauthorized_results : Specification
{
    CommandResult _result;
    CommandResult _unauthorizedResultWithoutReason;
    CommandResult _firstUnauthorizedResultWithReason;
    CommandResult _secondUnauthorizedResultWithReason;

    void Establish()
    {
        _result = CommandResult.Success(CorrelationId.New());
        _unauthorizedResultWithoutReason = CommandResult.Unauthorized(CorrelationId.New());
        _firstUnauthorizedResultWithReason = CommandResult.Unauthorized(CorrelationId.New(), "First reason");
        _secondUnauthorizedResultWithReason = CommandResult.Unauthorized(CorrelationId.New(), "Second reason");
    }

    void Because() => _result.MergeWith(_unauthorizedResultWithoutReason, _firstUnauthorizedResultWithReason, _secondUnauthorizedResultWithReason);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_first_non_empty_authorization_failure_reason() => _result.AuthorizationFailureReason.ShouldEqual("First reason");
}
