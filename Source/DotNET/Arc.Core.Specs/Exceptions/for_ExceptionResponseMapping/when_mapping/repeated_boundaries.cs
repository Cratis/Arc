// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class repeated_boundaries : given.an_exception_response_mapping
{
    void Because()
    {
        Map();
        Map();
    }

    [Fact] void should_map_each_failure_only_once() => _mapper.Received(2).TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>());
    [Fact] void should_keep_the_command_code() => _command.ErrorCode.ShouldEqual("namespace_closing");
    [Fact] void should_keep_the_query_code() => _query.ErrorCode.ShouldEqual("namespace_closing");
}
