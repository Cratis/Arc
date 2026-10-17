// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_throws_for_a_snapshot : given.a_guarded_sse_connection
{
    void Establish()
    {
        _streamingData = new[] { "secret" };
        _verdict = _ => throw new InvalidAuthorizationConfiguration("Session unavailable");
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_fail_closed() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_an_ordinary_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_reject_the_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
}
