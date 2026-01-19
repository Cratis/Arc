// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryExtensions;

public class when_creating_client_observable_from_subject : given.all_dependencies
{
    QueryContext _queryContext;
    BehaviorSubject<TestData> _subject;
    IClientObservable _result;

    void Establish()
    {
        _queryContext = new QueryContext("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _subject = new BehaviorSubject<TestData>(new TestData("Initial"));
    }

    void Because() => _result = ObservableQueryExtensions.CreateClientObservableFrom(
        _serviceProvider,
        _subject,
        _queryContext);

    [Fact] void should_return_client_observable() => _result.ShouldNotBeNull();
    [Fact] void should_return_client_observable_of_correct_type() => _result.ShouldBeOfExactType<ClientObservable<TestData>>();

    public record TestData(string Value);
}
