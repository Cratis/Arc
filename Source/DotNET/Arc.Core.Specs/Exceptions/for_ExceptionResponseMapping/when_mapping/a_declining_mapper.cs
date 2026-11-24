// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class a_declining_mapper : given.an_exception_response_mapping
{
    void Establish() => _mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(false);
    void Because() => Map();

    [Fact] void should_not_add_a_command_response() => ExceptionResponseMapping.ResponseFor(_command).ShouldBeNull();
    [Fact] void should_not_add_a_query_response() => ExceptionResponseMapping.ResponseFor(_query).ShouldBeNull();
    [Fact] void should_keep_command_detail() => _command.ExceptionMessages.ShouldContainOnly("private command detail");
    [Fact] void should_keep_query_detail() => _query.ExceptionMessages.ShouldContainOnly("private query detail");
}
