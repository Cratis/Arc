// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class a_recovery_failure : given.an_exception_response_mapping
{
    void Establish() => _response = new ExceptionResponse(HttpStatusCode.Conflict, "namespace_recovery_required");
    void Because() => Map();

    [Fact] void should_select_conflict_for_the_command() => EndpointRouteHelper.GetStatusCode(_command).ShouldEqual(HttpStatusCode.Conflict);
    [Fact] void should_select_conflict_for_the_query() => EndpointRouteHelper.GetStatusCode(_query).ShouldEqual(HttpStatusCode.Conflict);
    [Fact] void should_have_no_command_retry_hint() => ExceptionResponseMapping.ResponseFor(_command)!.Headers.ShouldBeEmpty();
    [Fact] void should_have_no_query_retry_hint() => ExceptionResponseMapping.ResponseFor(_query)!.Headers.ShouldBeEmpty();
    [Fact] void should_not_change_command_retry_policy() => _command.IsRetryable.ShouldBeFalse();
    [Fact] void should_not_change_query_retry_policy() => _query.IsRetryable.ShouldBeFalse();
}
