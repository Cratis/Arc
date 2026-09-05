// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_the_body_is_malformed : given.a_command_endpoint_mapper
{
    JsonException _parserException;
    CommandResult? _failure;
    object? _command;

    void Establish()
    {
        _parserException = CaptureJsonException("{ this is not valid json ");
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw _parserException);
    }

    async Task Because() => (_command, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_not_produce_a_command() => _command.ShouldBeNull();
    [Fact] void should_produce_a_failure_result() => _failure.ShouldNotBeNull();
    [Fact] void should_be_a_validation_failure() => _failure!.IsValid.ShouldBeFalse();
    [Fact] void should_stay_authorized() => _failure!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_carry_an_exception() => _failure!.HasExceptions.ShouldBeFalse();
    [Fact] void should_map_to_bad_request() => EndpointRouteHelper.GetStatusCode(_failure!.IsSuccess, _failure.IsAuthorized, _failure.IsValid).ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_leak_the_parser_message() => _failure!.ValidationResults.Any(validationResult => validationResult.Message.Contains(_parserException.Message)).ShouldBeFalse();
}
