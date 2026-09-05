// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

/// <summary>
/// An emission can pass its first cancellation check and then wait in interception while the client unsubscribes.
/// Unsubscription must cancel that subject subscription before disposing its guard scope, so the resumed emission
/// stops without consulting the application guard or reporting an authorization outcome.
/// </summary>
public class and_emission_resumes_before_guard_resolution : given.a_guarded_sse_connection
{
    TaskCompletionSource<IEnumerable<object>> _interceptionGate;
    TaskCompletionSource _interceptionStarted;

    void Establish()
    {
        _interceptionGate = new TaskCompletionSource<IEnumerable<object>>();
        _interceptionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _interceptionStarted.TrySetResult();
                return _interceptionGate.Task;
            });
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await _interceptionStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Unsubscribe(FirstQueryId);
        var logCallsBeforeRelease = LogCallCount;
        _interceptionGate.SetResult(["item-a"]);

        await WaitForLogAfter(logCallsBeforeRelease);
    });

    [Fact] void should_not_invoke_the_application_guard() => _guardCalls.ShouldBeEmpty();
    [Fact] void should_not_send_the_late_query_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_only_unregister_the_explicitly_unsubscribed_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

    int LogCallCount => _logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(ILogger.Log));

    async Task WaitForLogAfter(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (LogCallCount <= count)
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
