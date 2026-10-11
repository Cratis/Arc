// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_deadline_expires_after_suppression : given.a_request
{
    CancellationToken _firstToken;

    void Establish()
    {
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async call =>
        {
            var context = call.Arg<ObservableQueryEmissionContext>();
            _guardContexts.Add(context);
            if (_guardContexts.Count == 1)
            {
                _firstToken = context.CancellationToken;
                _guardEntered.TrySetResult();
            }
            else
            {
                var expired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = context.CancellationToken.Register(() => expired.TrySetResult());
                await expired.Task;
            }

            return ObservableQueryEmissionVerdict.Suppress;
        });
    }

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("first");
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(1)), _request);
        await _guardEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        subject.OnNext("second");
        _response = await response.WaitAsync(TimeSpan.FromSeconds(3));
        subject.HasObservers.ShouldBeFalse();
    }

    [Fact] void should_use_one_deadline_for_all_candidates() => _guardContexts.TrueForAll(context => context.CancellationToken == _firstToken).ShouldBeTrue();
    [Fact] void should_return_timeout() => _response.StatusCode.ShouldEqual(HttpStatusCode.RequestTimeout);
    [Fact] void should_not_include_data() => _response.Result.Data.ShouldBeNull();
}
