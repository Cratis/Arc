// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultCommandResponseValueHandler.when_checking_can_handle;

public class with_a_different_type : given.a_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_commandContext, "not an AggregateRootCommitResult");

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
