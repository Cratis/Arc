// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_mapping;

public class built_in_failures : given.an_exception_response_mapping
{
    CommandResult[] _commands;
    QueryResult[] _queries;

    void Establish()
    {
        _commands = [
            new() { ExceptionResponseCandidate = _command.ExceptionResponseCandidate, ExceptionMessages = ["denied"], IsAuthorized = false },
            new() { ExceptionResponseCandidate = _command.ExceptionResponseCandidate, ExceptionMessages = ["invalid"], ValidationResults = [ValidationResult.Error("invalid")] },
            new() { ExceptionResponseCandidate = _command.ExceptionResponseCandidate, ExceptionMessages = ["retryable"], IsRetryable = true },
            CommandResult.Success(_command.CorrelationId), CommandResult.Error(_command.CorrelationId, "untrusted client string")];
        _queries = [
            new() { ExceptionResponseCandidate = _query.ExceptionResponseCandidate, ExceptionMessages = ["denied"], IsAuthorized = false },
            new() { ExceptionResponseCandidate = _query.ExceptionResponseCandidate, ExceptionMessages = ["invalid"], ValidationResults = [ValidationResult.Error("invalid")] },
            new() { ExceptionResponseCandidate = _query.ExceptionResponseCandidate, ExceptionMessages = ["retryable"], IsRetryable = true },
            new() { ExceptionResponseCandidate = _query.ExceptionResponseCandidate, ExceptionMessages = ["pending"], IsReady = false },
            QueryResult.Success(_query.CorrelationId), QueryResult.Error(_query.CorrelationId, "untrusted client string")];
    }

    void Because()
    {
        foreach (var result in _commands) ExceptionResponseMapping.Apply(result, _services, NullLogger.Instance);
        foreach (var result in _queries) ExceptionResponseMapping.Apply(result, _services, NullLogger.Instance);
    }

    [Fact] void should_not_offer_ineligible_failures_to_the_mapper() => _mapper.DidNotReceive().TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>());
    [Fact] void should_not_add_command_codes() => _commands.All(result => result.ErrorCode is null).ShouldBeTrue();
    [Fact] void should_not_add_query_codes() => _queries.All(result => result.ErrorCode is null).ShouldBeTrue();
}
