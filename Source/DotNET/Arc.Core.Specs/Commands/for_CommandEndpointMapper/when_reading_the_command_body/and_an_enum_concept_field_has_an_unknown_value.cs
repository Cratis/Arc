// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_an_enum_concept_field_has_an_unknown_value : given.a_command_endpoint_mapper
{
    ArgumentException _converterException;
    CommandResult? _failure;
    object? _command;

    void Establish()
    {
        // An unknown value for an enum-backed concept property runs the concept converter, which calls Enum.Parse
        // and throws an ArgumentException that escapes the JSON deserializer unwrapped.
        _converterException = new ArgumentException("Requested value 'Unknown' was not found.");
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw _converterException);
    }

    async Task Because() => (_command, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_not_produce_a_command() => _command.ShouldBeNull();
    [Fact] void should_be_a_validation_failure() => _failure!.IsValid.ShouldBeFalse();
    [Fact] void should_stay_authorized() => _failure!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_carry_an_exception() => _failure!.HasExceptions.ShouldBeFalse();
    [Fact] void should_map_to_bad_request() => EndpointRouteHelper.GetStatusCode(_failure!.IsSuccess, _failure.IsAuthorized, _failure.IsValid).ShouldEqual(HttpStatusCode.BadRequest);
}
