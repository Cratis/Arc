// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_subscribe_preserves_the_receipt : given.a_guarded_websocket_connection
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _observed;
    DateTimeOffset? _after;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received);
        _context.RequestServices.Returns(new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider());
        _queryPipeline.Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>()).Returns(call =>
            {
                clock.GetUtcNow().Returns(_received.AddMinutes(1));

                // A decorated pipeline forwards to the built-in public entry after the subscribe was received.
                using var forwarded = OperationContextScope.BeginPipeline(call.ArgAt<IServiceProvider>(4));
                _observed = new OperationContextAccessor().ReceivedAt;
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = _subject;
                return Task.FromResult(result);
            });
    }

    async Task Because()
    {
        await RunConnection(() => Task.CompletedTask);
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_keep_the_subscribe_receipt_through_a_decorator() => _observed.ShouldEqual(_received);
    [Fact] void should_clear_the_receipt_after_the_connection() => _after.ShouldBeNull();
}
