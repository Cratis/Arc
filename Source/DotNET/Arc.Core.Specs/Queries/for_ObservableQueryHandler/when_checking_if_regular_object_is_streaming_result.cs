// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_checking_if_regular_object_is_streaming_result : given.an_observable_query_handler
{
    string _data;
    bool _result;

    void Establish()
    {
        _data = "regular string";
    }

    void Because() => _result = _handler.IsStreamingResult(_data);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
