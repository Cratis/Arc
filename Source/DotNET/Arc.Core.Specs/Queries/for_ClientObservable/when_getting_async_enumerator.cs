// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable;

public class when_getting_async_enumerator : given.a_client_observable
{
    IAsyncEnumerator<TestData> _enumerator;

    void Because() => _enumerator = _clientObservable.GetAsyncEnumerator();

    [Fact] void should_return_enumerator() => _enumerator.ShouldNotBeNull();
    [Fact] void should_return_observable_async_enumerator() => _enumerator.ShouldBeOfExactType<ObservableAsyncEnumerator<TestData>>();
}
