// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter;

public class when_checking_if_object_result_has_no_streaming_result : given.a_controller_observable_query_adapter
{
    ObjectResult _objectResult;
    bool _resultIsSubject;
    bool _resultIsAsyncEnumerable;

    void Establish()
    {
        _objectResult = new ObjectResult("regular string");
    }

    void Because()
    {
        _resultIsSubject = _objectResult.IsSubjectResult();
        _resultIsAsyncEnumerable = _objectResult.IsAsyncEnumerableResult();
    }

    [Fact] void should_not_be_subject() => _resultIsSubject.ShouldBeFalse();
    [Fact] void should_not_be_async_enumerable() => _resultIsAsyncEnumerable.ShouldBeFalse();
}
