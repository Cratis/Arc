// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Threading.Channels;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_subscribing_to_a_string_subject : given.an_observable_query_demultiplexer
{
    QueryResult _initial;
    QueryResult _next;

    void Establish() => _queryContextManager.Current.Returns(new QueryContext(
        "[TestApp].[TestQuery]", CorrelationId.New(), new Paging(2, 10, true), Sorting.None)
    {
        TotalItems = 73
    });

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("Initial");
        var results = Channel.CreateUnbounded<QueryResult>();
        using var subscription = _hub.SubscribeToStreamingData(
            Substitute.For<IHttpRequestContext>(),
            subject,
            "query-1",
            new PagingInfo(2, 10, 73),
            null,
            CorrelationId.New(),
            new ObservableQuerySubscriptionIdentity("[TestApp].[TestQuery]", QueryArguments.Empty, null),
            (result, _) =>
            {
                results.Writer.TryWrite(result);
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        _initial = await results.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        subject.OnNext("Updated");
        _next = await results.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_forward_the_initial_string() => _initial.Data.ShouldEqual("Initial");
    [Fact] void should_forward_the_updated_string() => _next.Data.ShouldEqual("Updated");
    [Fact] void should_not_report_paging_on_the_initial_string() => _initial.Paging.ShouldEqual(PagingInfo.NotPaged);
    [Fact] void should_not_report_paging_on_subsequent_strings() => _next.Paging.ShouldEqual(PagingInfo.NotPaged);
}
