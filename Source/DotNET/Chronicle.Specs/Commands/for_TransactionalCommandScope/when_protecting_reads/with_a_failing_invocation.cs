// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

public class with_a_failing_invocation : given.a_read_protection_pipeline
{
    Exception? _afterExit;
    void Establish() => _handle = _ => throw new Failure();
    async Task Because()
    {
        _result = await Execute();
        _afterExit = ProbeFrame();
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_roll_back_the_owned_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_release_enforcement() => _afterExit.ShouldBeNull();
    public class Failure : Exception;
}
