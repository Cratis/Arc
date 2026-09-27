// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class paged_multiplexed_subscriptions : an_observable_query_demultiplexer
{
    protected const string FirstQuery = "Queries.First";
    protected const string SecondQuery = "Queries.Second";
    protected const string FirstId = "first";
    protected const string SecondId = "second";
    protected const int Page = 2;
    protected const int Size = 20;
    protected QueryContext FirstContext;
    protected QueryContext SecondContext;
    protected BehaviorSubject<IEnumerable<string>> FirstSubject;
    protected BehaviorSubject<IEnumerable<string>> SecondSubject;

    void Establish()
    {
        UseRealQueryContextManager();
        FirstContext = new QueryContext(new FullyQualifiedQueryName(FirstQuery), CorrelationId.New(), new Paging(Page, Size, true), Sorting.None) { TotalItems = 137 };
        SecondContext = new QueryContext(new FullyQualifiedQueryName(SecondQuery), CorrelationId.New(), new Paging(Page, Size, true), Sorting.None) { TotalItems = 23 };
        FirstSubject = new BehaviorSubject<IEnumerable<string>>([]);
        SecondSubject = new BehaviorSubject<IEnumerable<string>>([]);

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                // An AsyncLocal assignment inside an awaited pipeline does not flow back to the subscriber.
                await Task.Yield();
                var first = callInfo.ArgAt<FullyQualifiedQueryName>(0).Value == FirstQuery;
                var queryContext = first ? FirstContext : SecondContext;
                _queryContextManager.Set(queryContext);
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = first ? FirstSubject : SecondSubject;
                result.AuthorizedQueryContext = queryContext;
                result.Paging = new PagingInfo(Page, Size, 0);
                return result;
            });
    }

    protected static JsonElement PayloadFor(IEnumerable<ObservableQueryHubMessage> messages, string queryId, int emission) =>
        (JsonElement)messages.Where(message => message.Type == ObservableQueryHubMessageType.QueryResult && message.QueryId == queryId)
            .ElementAt(emission).Payload!;

    protected static JsonElement PagingFor(IEnumerable<ObservableQueryHubMessage> messages, string queryId, int emission) =>
        Property(PayloadFor(messages, queryId, emission), "paging");

    protected static JsonElement Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value : element.GetProperty(char.ToUpperInvariant(name[0]) + name[1..]);

    protected static void AssertPaging(JsonElement paging, int totalItems)
    {
        Property(paging, "page").GetInt32().ShouldEqual(Page);
        Property(paging, "size").GetInt32().ShouldEqual(Size);
        Property(paging, "totalItems").GetInt32().ShouldEqual(totalItems);
    }
}
