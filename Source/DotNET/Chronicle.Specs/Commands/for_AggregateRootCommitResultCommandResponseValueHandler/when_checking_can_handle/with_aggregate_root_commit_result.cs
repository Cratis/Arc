// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultCommandResponseValueHandler.when_checking_can_handle;

public class with_aggregate_root_commit_result : given.a_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_commandContext, AggregateRootCommitResult.Successful());

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
