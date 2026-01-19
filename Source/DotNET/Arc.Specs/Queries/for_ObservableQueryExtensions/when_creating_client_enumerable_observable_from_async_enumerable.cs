// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryExtensions;

public class when_creating_client_enumerable_observable_from_async_enumerable : given.all_dependencies
{
    TestAsyncEnumerable _enumerable;
    IClientEnumerableObservable _result;

    void Establish()
    {
        _enumerable = new TestAsyncEnumerable();
    }

    void Because() => _result = ObservableQueryExtensions.CreateClientEnumerableObservableFrom(
        _serviceProvider,
        _enumerable);

    [Fact] void should_return_client_observable() => _result.ShouldNotBeNull();
    [Fact] void should_return_client_enumerable_observable_of_correct_type() => _result.ShouldBeOfExactType<ClientEnumerableObservable<TestData>>();

    public record TestData(string Value);

    class TestAsyncEnumerable : IAsyncEnumerable<TestData>
    {
        public async IAsyncEnumerator<TestData> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new TestData("test");
        }
    }
}
