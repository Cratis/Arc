// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableAsyncEnumerator.when_moving_next;

public class and_the_token_is_already_cancelled : Specification
{
    Subject<int> _subject;
    CancellationTokenSource _cts;
    ObservableAsyncEnumerator<int> _enumerator;
    Exception _error;
    bool _result;

    void Establish()
    {
        _subject = new();
        _cts = new();
        _cts.Cancel();
        _enumerator = new(_subject, _cts.Token);
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

    [Fact] void should_throw_operation_cancelled() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_return_an_item() => _result.ShouldBeFalse();

    async Task Destroy()
    {
        await _enumerator.DisposeAsync();
        _subject.Dispose();
        _cts.Dispose();
    }
}
