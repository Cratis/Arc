// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class an_invalid_response : given.an_exception_response_mapping
{
    void Establish() => _response = new ExceptionResponse(HttpStatusCode.OK, "not_a_failure", new Dictionary<string, string> { ["Retry-After"] = "5" });
    void Because() => Map();

    [Fact] void should_not_partially_apply_the_command_code() => _command.ErrorCode.ShouldBeNull();
    [Fact] void should_not_partially_apply_the_query_code() => _query.ErrorCode.ShouldBeNull();
    [Fact] void should_not_partially_apply_the_command_headers() => ExceptionResponseMapping.ResponseFor(_command).ShouldBeNull();
    [Fact] void should_not_partially_apply_the_query_headers() => ExceptionResponseMapping.ResponseFor(_query).ShouldBeNull();
    [Fact] void should_keep_the_original_command_failure() => _command.ExceptionMessages.ShouldContainOnly("private command detail");
    [Fact] void should_keep_the_original_query_failure() => _query.ExceptionMessages.ShouldContainOnly("private query detail");
}
