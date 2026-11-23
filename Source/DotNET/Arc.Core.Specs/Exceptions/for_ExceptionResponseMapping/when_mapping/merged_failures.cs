// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class merged_failures : given.an_exception_response_mapping
{
    Exception _firstCommandException;
    Exception _firstQueryException;

    void Establish()
    {
        _firstCommandException = _command.ExceptionResponseCandidate!.Exception;
        _firstQueryException = _query.ExceptionResponseCandidate!.Exception;
        var command = CommandResult.Success(_command.CorrelationId);
        command.MergeWith(_command, CommandResult.Error(_command.CorrelationId, new Exception("later")));
        _command = command;
        var query = QueryResult.Success(_query.CorrelationId);
        query.MergeWith(_query, QueryResult.Error(_query.CorrelationId, new Exception("later")));
        _query = query;
    }

    void Because() => Map();

    [Fact] void should_offer_the_first_command_exception() => _mapper.Received(1).TryMap(_firstCommandException, out Arg.Any<ExceptionResponse>());
    [Fact] void should_offer_the_first_query_exception() => _mapper.Received(1).TryMap(_firstQueryException, out Arg.Any<ExceptionResponse>());
    [Fact] void should_keep_the_command_failure() => _command.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_query_failure() => _query.IsSuccess.ShouldBeFalse();
}
