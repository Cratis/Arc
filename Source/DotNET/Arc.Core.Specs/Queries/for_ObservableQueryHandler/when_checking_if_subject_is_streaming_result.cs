// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_checking_if_subject_is_streaming_result : given.an_observable_query_handler
{
    BehaviorSubject<string> _subject;
    bool _result;

    void Establish()
    {
        _subject = new BehaviorSubject<string>("test");
    }

    void Because() => _result = _handler.IsStreamingResult(_subject);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
