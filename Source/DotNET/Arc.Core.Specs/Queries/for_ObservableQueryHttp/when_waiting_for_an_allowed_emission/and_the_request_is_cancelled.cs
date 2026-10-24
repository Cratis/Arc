// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_request_is_cancelled : given.a_request
{
    CancellationTokenSource _cancellation;
    Subject<string> _subject;
    Exception _error;
    bool _subscribed;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();
        _request.RequestAborted.Returns(_cancellation.Token);
        _subject = new Subject<string>();
    }

    async Task Because()
    {
        var pending = ObservableQueryHttp.CreateResponse(_queryContext, _subject, new(true, TimeSpan.FromSeconds(2)), _request);
        _subscribed = _subject.HasObservers;
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await pending);
    }

    void Destroy()
    {
        _subject.Dispose();
        _cancellation.Dispose();
    }

    [Fact]
    void should_unsubscribe_on_cancellation()
    {
        _subscribed.ShouldBeTrue();
        _error.ShouldBeOfExactType<OperationCanceledException>();
        _subject.HasObservers.ShouldBeFalse();
    }
}
