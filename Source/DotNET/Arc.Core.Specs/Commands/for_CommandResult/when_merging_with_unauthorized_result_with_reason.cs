// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_merging_with_unauthorized_result_with_reason : Specification
{
    CommandResult _result;
    CommandResult _unauthorizedResult;

    void Establish()
    {
        _result = CommandResult.Success(CorrelationId.New());
        _unauthorizedResult = CommandResult.Unauthorized(CorrelationId.New(), "License required");
    }

    void Because() => _result.MergeWith(_unauthorizedResult);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_authorization_failure_reason() => _result.AuthorizationFailureReason.ShouldEqual("License required");
}
