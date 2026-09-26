// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_two_subscriptions_have_separate_receipts : given.a_guarded_sse_connection
{
    readonly ConcurrentQueue<DateTimeOffset?> _receipts = new();
    readonly DateTimeOffset _first = new(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);
    readonly DateTimeOffset _second = new(2026, 5, 6, 7, 8, 10, TimeSpan.Zero);

    void Establish()
    {
        _queryPipeline.Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>()).Returns(_ =>
            {
                _receipts.Enqueue(new OperationContextAccessor().ReceivedAt);
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = _subject;
                return Task.FromResult(result);
            });
    }

    async Task Because()
    {
        var connection = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));
        try
        {
            await _hub.HandleSSESubscribe(Subscribe(FirstQueryId, _first));
            await _hub.HandleSSESubscribe(Subscribe(SecondQueryId, _second));
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connection;
        }
    }

    [Fact]
    void should_capture_a_new_receipt_for_each_subscribe_post()
    {
        var receipts = _receipts.ToArray();
        receipts.Length.ShouldEqual(2);
        receipts[0].ShouldEqual(_first);
        receipts[1].ShouldEqual(_second);
    }

    IHttpRequestContext Subscribe(string queryId, DateTimeOffset now)
    {
        var context = CreateSubscribeContext(queryId);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        context.RequestServices.Returns(new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider());
        return context;
    }
}
