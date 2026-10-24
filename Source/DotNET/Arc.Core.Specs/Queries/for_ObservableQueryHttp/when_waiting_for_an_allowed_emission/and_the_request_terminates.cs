// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_request_terminates : given.a_request
{
    [Fact]
    async Task should_unsubscribe_on_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        _request.RequestAborted.Returns(cancellation.Token);
        using var subject = new Subject<string>();
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        subject.HasObservers.ShouldBeTrue();
        await cancellation.CancelAsync();
        var error = await Catch.Exception(async () => await response);
        error.ShouldBeOfExactType<OperationCanceledException>();
        subject.HasObservers.ShouldBeFalse();
    }

    [Fact]
    async Task should_report_completion_without_an_eligible_emission()
    {
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(Task.FromResult(ObservableQueryEmissionVerdict.Suppress));
        using var subject = new Subject<string>();
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        subject.OnNext("withheld");
        subject.OnCompleted();
        var completed = await response;
        completed.StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
        completed.Result.Data.ShouldBeNull();
        subject.HasObservers.ShouldBeFalse();
    }

    [Fact]
    async Task should_not_outlive_interception_on_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        _request.RequestAborted.Returns(cancellation.Token);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<IEnumerable<object>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(_ =>
        {
            entered.TrySetResult();
            return release.Task;
        });
        using var subject = new BehaviorSubject<string>("in-flight");
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(1), global::Xunit.TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        response.IsCompleted.ShouldBeFalse();
        release.SetResult(["intercepted"]);
        var error = await Catch.Exception(async () => await response);
        error.ShouldBeOfExactType<OperationCanceledException>();
        subject.HasObservers.ShouldBeFalse();
    }
}
