// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_pipeline_returns_live_paging_context : given.paged_multiplexed_subscriptions
{
    IHttpRequestContext _connection;
    IHttpRequestContext _subscribe;
    CancellationTokenSource _cancellation;
    ConcurrentQueue<ObservableQueryHubMessage> _messages;
    string _connectionId;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();
        _messages = [];
        _connectionId = string.Empty;
        _connection = Substitute.For<IHttpRequestContext>();
        _connection.RequestAborted.Returns(_cancellation.Token);
        _connection.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _connection.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var text = callInfo.Arg<string>();
                if (text.StartsWith("data: ", StringComparison.Ordinal))
                {
                    var message = JsonSerializer.Deserialize<ObservableQueryHubMessage>(text["data: ".Length..].Trim(), _arcOptions.Value.JsonSerializerOptions);
                    if (message is not null)
                    {
                        _messages.Enqueue(message);
                        _signals.Signal();
                    }
                }
                return Task.CompletedTask;
            });
        _subscribe = Substitute.For<IHttpRequestContext>();
        _subscribe.RequestAborted.Returns(CancellationToken.None);
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connection);
        try
        {
            await WaitFor(() => _messages.Any(message => message.Type == ObservableQueryHubMessageType.Connected));
            _connectionId = ((JsonElement)_messages.First(message => message.Type == ObservableQueryHubMessageType.Connected).Payload!).GetString()!;

            await Subscribe(FirstId, FirstQuery);
            await Subscribe(SecondId, SecondQuery);
            await WaitFor(() => ResultCount(FirstId) == 1 && ResultCount(SecondId) == 1);

            FirstContext.TotalItems = 141;
            FirstSubject.OnNext(["first-item"]);
            await WaitFor(() => ResultCount(FirstId) == 2);
            SecondSubject.OnNext(["second-item"]);
            await WaitFor(() => ResultCount(SecondId) == 2);
        }
        finally
        {
            await _cancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_start_with_an_empty_result() => Property(PayloadFor(_messages, FirstId, 0), "data").GetArrayLength().ShouldEqual(0);
    [Fact] void should_include_paging_on_first_empty_emission() => AssertPaging(PagingFor(_messages, FirstId, 0), 137);
    [Fact] void should_include_updated_total_items_on_later_emission() => AssertPaging(PagingFor(_messages, FirstId, 1), 141);
    [Fact] void should_keep_the_second_subscription_independent() => AssertPaging(PagingFor(_messages, SecondId, 1), 23);
    [Fact] void should_not_depend_on_ambient_context() => _queryContextManager.Current.ShouldEqual(QueryContext.NotSet);

    async Task Subscribe(string queryId, string queryName)
    {
        _subscribe.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId, queryId, new ObservableQuerySubscriptionRequest(queryName) { Page = Page, PageSize = Size })));
        await _hub.HandleSSESubscribe(_subscribe);
    }

    int ResultCount(string queryId) => _messages.Count(message => message.Type == ObservableQueryHubMessageType.QueryResult && message.QueryId == queryId);
}
