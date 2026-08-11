// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult;

public class when_merging_with_unauthorized_result_and_reason_already_set : Specification
{
    CommandResult _result;
    CommandResult _unauthorizedResult;

    void Establish()
    {
        _result = CommandResult.Unauthorized(CorrelationId.New(), "Original reason");
        _unauthorizedResult = CommandResult.Unauthorized(CorrelationId.New(), "Other reason");
    }

    void Because() => _result.MergeWith(_unauthorizedResult);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_keep_already_set_authorization_failure_reason() => _result.AuthorizationFailureReason.ShouldEqual("Original reason");
}
