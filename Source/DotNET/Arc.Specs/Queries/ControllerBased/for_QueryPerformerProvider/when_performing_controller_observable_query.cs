// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider;

public class when_performing_controller_observable_query : a_controller_query_performer_provider
{
    QueryContext _queryContext;
    IQueryPerformer? _performer;
    ITestEventStores _eventStores;
    ISubject<IEnumerable<string>> _expectedSubject;
    object? _result;

    void Establish()
    {
        _provider.TryGetPerformerFor(QueryName, out _performer);

        _eventStores = Substitute.For<ITestEventStores>();
        _expectedSubject = new BehaviorSubject<IEnumerable<string>>([]);
        _eventStores.ObserveFor("north").Returns(_expectedSubject);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(_eventStores)
            .BuildServiceProvider();

        _queryContext = new QueryContext(
            QueryName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments
            {
                ["tenant"] = "north"
            },
            [serviceProvider]);
    }

    async Task Because() => _result = await _performer!.Perform(_queryContext);

    [Fact] void should_call_controller_query_method_with_argument() => _eventStores.Received(1).ObserveFor("north");
    [Fact] void should_return_subject_from_controller_query() => _result.ShouldEqual(_expectedSubject);
}