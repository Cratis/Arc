// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_guard_suppresses_the_first_value : given.a_request
{
    void Establish()
    {
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(call =>
        {
            _guardContexts.Add(call.Arg<ObservableQueryEmissionContext>());
            _guardEntered.TrySetResult();
            return Task.FromResult(_guardContexts.Count == 1 ? ObservableQueryEmissionVerdict.Suppress : ObservableQueryEmissionVerdict.Allow);
        });
    }

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("withheld");
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        await _guardEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        response.IsCompleted.ShouldBeFalse();
        subject.OnNext("eligible");
        _response = await response.WaitAsync(TimeSpan.FromSeconds(3));
        subject.HasObservers.ShouldBeFalse();
    }

    [Fact] void should_return_the_first_allowed_value() => _response.Result.Data.ShouldEqual("eligible-intercepted");
    [Fact] void should_keep_first_emission_true_after_suppression() => _guardContexts.TrueForAll(context => context.IsFirstEmission).ShouldBeTrue();
}
