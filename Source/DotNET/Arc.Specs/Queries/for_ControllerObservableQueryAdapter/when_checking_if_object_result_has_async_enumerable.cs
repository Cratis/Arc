// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter;

public class when_checking_if_object_result_has_async_enumerable : given.a_controller_observable_query_adapter
{
    ObjectResult _objectResult;
    bool _result;

    void Establish()
    {
        _objectResult = new ObjectResult(new TestAsyncEnumerable());
    }

    void Because() => _result = _objectResult.IsAsyncEnumerableResult();

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    class TestAsyncEnumerable : IAsyncEnumerable<TestData>
    {
        public async IAsyncEnumerator<TestData> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new TestData("test");
        }
    }
}
