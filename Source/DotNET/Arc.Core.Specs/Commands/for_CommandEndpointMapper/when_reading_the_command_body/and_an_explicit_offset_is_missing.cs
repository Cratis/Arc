// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands.for_ExplicitOffsetAttribute.given;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_an_explicit_offset_is_missing : given.a_command_endpoint_mapper
{
    CommandResult? _failure;

    void Establish()
    {
        var exception = Catch.Exception(() => JsonSerializer.Deserialize<date_inputs.OffsetInput>("""{"from":"2026-10-16T12:30"}""", new JsonSerializerOptions().ConfigureArcDefaults()));
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw exception);
    }

    async Task Because() => (_, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_report_the_member() => _failure!.ValidationResults.Single().Members.ShouldContainOnly("from");
    [Fact] void should_report_the_arc_refusal() => _failure!.ValidationResults.Single().Message.ShouldEqual("DateTime command property 'from' requires an explicit offset or Z.");
    [Fact] void should_report_malformed_request() => _failure!.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
}
