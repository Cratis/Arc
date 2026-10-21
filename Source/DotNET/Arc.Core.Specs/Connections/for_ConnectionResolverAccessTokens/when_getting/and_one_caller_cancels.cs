// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolverAccessTokens.when_getting;

public class and_one_caller_cancels : given.access_tokens
{
    TaskCompletionSource<ConnectionResolverAccessToken> _issued;
    CancellationToken _fetchCancellation;
    Exception _error;
    bool _fetchWasCanceled;
    bool _otherWaiterWasCompleted;
    int _calls;
    string _result;
    string _cached;

    void Establish()
    {
        _issued = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _provider.GetToken(_options, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _calls++;
            _fetchCancellation = call.Arg<CancellationToken>();
            return new ValueTask<ConnectionResolverAccessToken>(_issued.Task.WaitAsync(_fetchCancellation));
        });
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var canceling = _tokens.Get(cancellation.Token).AsTask();
        var waiting = _tokens.Get(CancellationToken.None).AsTask();
        await cancellation.CancelAsync();
        _error = await Catch.Exception(async () => await canceling.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _fetchWasCanceled = _fetchCancellation.IsCancellationRequested;
        _otherWaiterWasCompleted = waiting.IsCompleted;
        _issued.SetResult(new("the-token", _time.Now.AddHours(1)));
        _result = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _cached = await _tokens.Get(CancellationToken.None);
    }

    [Fact] void should_cancel_only_the_callers_wait() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_cancel_the_shared_fetch() => _fetchWasCanceled.ShouldBeFalse();
    [Fact] void should_leave_the_other_caller_waiting_for_the_token() => _otherWaiterWasCompleted.ShouldBeFalse();
    [Fact] void should_give_the_other_caller_the_token() => _result.ShouldEqual("the-token");
    [Fact] void should_cache_the_shared_token() => _cached.ShouldEqual("the-token");
    [Fact] void should_fetch_only_one_token() => _calls.ShouldEqual(1);
}
