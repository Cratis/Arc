// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class and_a_nested_collection_value_cannot_be_read : given.a_command_endpoint_mapper
{
    CommandResult? _failure;

    void Establish()
    {
        var exception = Catch.Exception(() => JsonSerializer.Deserialize<Envelope>("""{"Items":[{"Amount":1},{"Amount":2},{"Amount":"wrong"}]}"""));
        _context.ReadBodyAsJson(_commandType, Arg.Any<CancellationToken>()).Returns<Task<object?>>(_ => throw exception);
    }

    async Task Because() => (_, _failure) = await CommandEndpointMapper.ReadCommandBody(_context, _commandType, _correlationId, _logger);

    [Fact] void should_report_the_nested_member() => _failure!.ValidationResults.Single().Members.ShouldContainOnly("items[2].amount");
    [Fact] void should_keep_parser_details_private() => _failure!.ValidationResults.Single().Message.ShouldEqual("The request body could not be read or is not valid for this command.");

    public record Envelope(IEnumerable<SomeCommand> Items);
}
