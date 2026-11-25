// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class the_first_accepting_mapper : given.an_exception_response_mapping
{
    IExceptionResponseMapper _declining;
    IExceptionResponseMapper _later;

    void Establish()
    {
        _declining = Substitute.For<IExceptionResponseMapper>();
        _later = Substitute.For<IExceptionResponseMapper>();
        _services.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)).Returns(new KnownInstancesOf<IExceptionResponseMapper>([_declining, _mapper, _later]));
    }

    void Because() => Map();

    [Fact] void should_try_the_declining_mapper_for_each_failure() => _declining.Received(2).TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>());
    [Fact] void should_not_try_mappers_after_acceptance() => _later.DidNotReceive().TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>());
    [Fact] void should_use_the_first_accepted_command_response() => _command.ErrorCode.ShouldEqual("namespace_closing");
    [Fact] void should_use_the_first_accepted_query_response() => _query.ErrorCode.ShouldEqual("namespace_closing");
}
