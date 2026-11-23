// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class a_resolution_failure : given.an_exception_response_mapping
{
    void Establish() => _services.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(_ => throw new InvalidOperationException("resolution failed"));
    void Because() => Map();

    [Fact] void should_keep_the_original_command_failure() => _command.ExceptionMessages.ShouldContainOnly("private command detail");
    [Fact] void should_keep_the_original_query_failure() => _query.ExceptionMessages.ShouldContainOnly("private query detail");
    [Fact] void should_not_add_command_metadata() => _command.ErrorCode.ShouldBeNull();
    [Fact] void should_not_add_query_metadata() => _query.ErrorCode.ShouldBeNull();
}
