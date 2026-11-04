// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_moving_next;

public class and_the_token_is_cancelled_while_waiting : Specification
{
    Subject<int> _subject;
    CancellationTokenSource _cts;
    ObservableAsyncEnumerator<int> _enumerator;
    Task<bool> _move;
    bool _pendingBeforeAction;
    Exception _error;

    void Establish()
    {
        _subject = new();
        _cts = new();
        _enumerator = new(_subject, _cts.Token);
        _move = _enumerator.MoveNextAsync().AsTask();
        _pendingBeforeAction = !_move.IsCompleted;
    }

    async Task Because()
    {
        await _cts.CancelAsync();
        _error = await Catch.Exception(async () => await _move.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact] void should_have_been_waiting_before_cancellation() => _pendingBeforeAction.ShouldBeTrue();
    [Fact] void should_end_the_pending_move_with_operation_cancelled() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_carry_the_cancelled_token() => ((OperationCanceledException)_error).CancellationToken.ShouldEqual(_cts.Token);

    async Task Destroy()
    {
        await _enumerator.DisposeAsync();
        _subject.Dispose();
        _cts.Dispose();
    }
}
