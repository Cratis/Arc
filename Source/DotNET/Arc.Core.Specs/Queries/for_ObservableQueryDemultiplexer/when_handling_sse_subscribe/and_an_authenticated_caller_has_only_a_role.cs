// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_an_authenticated_caller_has_only_a_role : given.an_sse_connection_owned_by_a_caller
{
    int _subscribeStatus;
    int _unsubscribeStatus;

    void Establish()
    {
        _owner = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Reader")], "test"));
        _anotherCaller = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Reader")], "test"));
        _connectionContext.User.Returns(_owner);
    }

    async Task Because() => await RunConnection(async () =>
    {
        await _hub.HandleSSESubscribe(SubscribeContextFor(_anotherCaller));
        _subscribeStatus = _statusCode;
        await _hub.HandleSSEUnsubscribe(UnsubscribeContextFor(_anotherCaller));
        _unsubscribeStatus = _statusCode;
        _subject.OnNext(["an-item"]);
    });

    [Fact] void should_not_accept_the_subscribe_request() => _subscribeStatus.ShouldEqual(404);
    [Fact] void should_not_accept_the_unsubscribe_request() => _unsubscribeStatus.ShouldEqual(404);
    [Fact] void should_not_write_query_results() => CountQueryResults().ShouldEqual(0);
    [Fact] void should_not_perform_the_query() => _queryPipeline.DidNotReceive().Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
}
