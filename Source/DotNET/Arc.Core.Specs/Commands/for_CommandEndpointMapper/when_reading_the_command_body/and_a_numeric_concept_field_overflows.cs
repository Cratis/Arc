// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_a_numeric_concept_field_overflows : given.a_command_endpoint_mapper
{
    OverflowException _converterException;
    CommandResult? _failure;
    object? _command;

    void Establish()
    {
        // An out-of-range value for a numeric-backed concept property runs the concept converter, which calls
        // System.Convert.ChangeType and throws an OverflowException that escapes the JSON deserializer unwrapped.
        _converterException = new OverflowException("Value was either too large or too small for an Int32.");
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw _converterException);
    }

    async Task Because() => (_command, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_not_produce_a_command() => _command.ShouldBeNull();
    [Fact] void should_be_a_validation_failure() => _failure!.IsValid.ShouldBeFalse();
    [Fact] void should_stay_authorized() => _failure!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_carry_an_exception() => _failure!.HasExceptions.ShouldBeFalse();
    [Fact] void should_map_to_bad_request() => EndpointRouteHelper.GetStatusCode(_failure!.IsSuccess, _failure.IsAuthorized, _failure.IsValid).ShouldEqual(HttpStatusCode.BadRequest);
}
