// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_moving_next;

public class and_the_enumerator_is_disposed_while_waiting : Specification
{
    Subject<int> _subject;
    ObservableAsyncEnumerator<int> _enumerator;
    Task<bool> _move;
    bool _pendingBeforeAction;
    bool _result;

    void Establish()
    {
        _subject = new();
        _enumerator = new(_subject, CancellationToken.None);
        _move = _enumerator.MoveNextAsync().AsTask();
        _pendingBeforeAction = !_move.IsCompleted;
    }

    async Task Because()
    {
        await _enumerator.DisposeAsync();
        _subject.OnNext(42);
        _result = await _move.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact] void should_have_been_waiting_before_disposal() => _pendingBeforeAction.ShouldBeTrue();
    [Fact] void should_end_the_pending_move_with_false() => _result.ShouldBeFalse();

    async Task Destroy()
    {
        await _enumerator.DisposeAsync();
        _subject.Dispose();
    }
}
