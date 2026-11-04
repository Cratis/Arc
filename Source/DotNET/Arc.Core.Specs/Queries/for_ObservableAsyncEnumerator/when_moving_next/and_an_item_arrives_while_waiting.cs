// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_moving_next;

public class and_an_item_arrives_while_waiting : Specification
{
    Subject<int> _subject;
    ObservableAsyncEnumerator<int> _enumerator;
    Task<bool> _move;
    bool _result;

    void Establish()
    {
        _subject = new();
        _enumerator = new(_subject, CancellationToken.None);
        _move = _enumerator.MoveNextAsync().AsTask();
    }

    async Task Because()
    {
        _subject.OnNext(42);
        _result = await _move.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_expose_the_item_as_current() => _enumerator.Current.ShouldEqual(42);

    async Task Destroy()
    {
        await _enumerator.DisposeAsync();
        _subject.Dispose();
    }
}
