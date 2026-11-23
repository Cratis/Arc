// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class a_temporary_failure : given.an_exception_response_mapping
{
    void Because() => Map();

    [Fact] void should_select_service_unavailable_for_the_command() => EndpointRouteHelper.GetStatusCode(_command).ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_select_service_unavailable_for_the_query() => EndpointRouteHelper.GetStatusCode(_query).ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_keep_the_command_code() => _command.ErrorCode.ShouldEqual("namespace_closing");
    [Fact] void should_keep_the_query_code() => _query.ErrorCode.ShouldEqual("namespace_closing");
    [Fact] void should_redact_the_command() => _command.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
    [Fact] void should_redact_the_query() => _query.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
    [Fact] void should_keep_command_retry_after() => ExceptionResponseMapping.ResponseFor(_command)!.Headers["Retry-After"].ShouldEqual("5");
    [Fact] void should_keep_query_retry_after() => ExceptionResponseMapping.ResponseFor(_query)!.Headers["Retry-After"].ShouldEqual("5");
}
