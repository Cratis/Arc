// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Types;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class without_mappers : given.an_exception_response_mapping
{
    void Establish() => _services.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(null);
    void Because() => Map();

    [Fact] void should_keep_the_command_status() => EndpointRouteHelper.GetStatusCode(_command).ShouldEqual(HttpStatusCode.InternalServerError);
    [Fact] void should_keep_the_query_status() => EndpointRouteHelper.GetStatusCode(_query).ShouldEqual(HttpStatusCode.InternalServerError);
    [Fact] void should_keep_command_disclosure_policy() => _command.ExceptionMessages.ShouldContainOnly("private command detail");
    [Fact] void should_keep_query_disclosure_policy() => _query.ExceptionMessages.ShouldContainOnly("private query detail");
    [Fact] void should_not_add_a_command_code() => _command.ErrorCode.ShouldBeNull();
    [Fact] void should_not_add_a_query_code() => _query.ErrorCode.ShouldBeNull();
}
