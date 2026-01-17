// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter;

public class when_checking_if_result_is_subject : given.a_controller_observable_query_adapter
{
    ObjectResult _objectResult;
    bool _result;

    void Establish()
    {
        var subject = new System.Reactive.Subjects.BehaviorSubject<TestData>(new TestData("test"));
        _objectResult = new ObjectResult(subject);
    }

    void Because() => _result = _objectResult.IsSubjectResult();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
