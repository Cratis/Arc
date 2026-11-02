// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_OperationSnapshot.when_nesting;

public class and_a_continuation_outlives_the_inner_frame : given.an_operation_snapshot_environment
{
    OperationSnapshot _outer;
    OperationSnapshot _seenByContinuation;

    async Task Because()
    {
        _outer = OperationSnapshot.Begin(_services, () => null);
        var inner = OperationSnapshot.Begin(_services, () => null);
        var gate = new TaskCompletionSource();
        var continuation = Task.Run(async () =>
        {
            await gate.Task;
            _seenByContinuation = OperationSnapshot.Current;
        });

        inner.Dispose();
        gate.SetResult();
        await continuation;
        _outer.Dispose();
    }

    [Fact] void should_not_see_a_snapshot() => _seenByContinuation.ShouldBeNull();
}
