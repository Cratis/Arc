// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_waiting_for_an_allowed_emission;

public class and_the_request_is_cancelled_during_interception : given.a_request
{
    CancellationTokenSource _cancellation;
    BehaviorSubject<string> _subject;
    TaskCompletionSource _entered;
    TaskCompletionSource<IEnumerable<object>> _release;
    Exception _error;
    bool _completedWhileIntercepting;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();
        _request.RequestAborted.Returns(_cancellation.Token);
        _entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new TaskCompletionSource<IEnumerable<object>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(_ =>
        {
            _entered.TrySetResult();
            return _release.Task;
        });
        _subject = new BehaviorSubject<string>("in-flight");
    }

    async Task Because()
    {
        var pending = ObservableQueryHttp.CreateResponse(_queryContext, _subject, new(true, TimeSpan.FromSeconds(2)), _request);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(1), global::Xunit.TestContext.Current.CancellationToken);
        await _cancellation.CancelAsync();
        _completedWhileIntercepting = pending.IsCompleted;
        _release.SetResult(["intercepted"]);
        _error = await Catch.Exception(async () => await pending);
    }

    void Destroy()
    {
        _subject.Dispose();
        _cancellation.Dispose();
    }

    [Fact]
    void should_not_outlive_interception_on_cancellation()
    {
        _completedWhileIntercepting.ShouldBeFalse();
        _error.ShouldBeOfExactType<OperationCanceledException>();
        _subject.HasObservers.ShouldBeFalse();
    }
}
