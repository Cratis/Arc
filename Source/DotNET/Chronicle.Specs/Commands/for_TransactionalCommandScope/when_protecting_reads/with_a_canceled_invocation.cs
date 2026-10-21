// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

public class with_a_canceled_invocation : given.a_read_protection_pipeline
{
    readonly CancellationTokenSource _cancellation = new();
    Exception? _afterExit;
    void Establish() => _handle = async context =>
    {
        await _cancellation.CancelAsync();
        context.CancellationToken.ThrowIfCancellationRequested();
        return null;
    };
    async Task Because()
    {
        _result = await Execute(cancellationToken: _cancellation.Token);
        _afterExit = ProbeFrame();
    }
    void Destroy() => _cancellation.Dispose();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_roll_back_the_owned_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_release_enforcement() => _afterExit.ShouldBeNull();
}
