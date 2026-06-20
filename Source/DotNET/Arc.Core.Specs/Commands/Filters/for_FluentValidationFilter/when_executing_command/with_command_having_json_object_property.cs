// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_command_having_json_object_property : given.a_fluent_validation_filter
{
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        // JsonNode children hold a back-reference to their parent, forming a cycle in the object graph.
        // Walking it without cycle detection recurses forever and overflows the stack.
        var payload = new JsonObject
        {
            ["id"] = "abc",
            ["nested"] = new JsonObject { ["value"] = 42 }
        };
        var command = new CommandWithJsonObject(payload);
        _context = new CommandContext(_correlationId, typeof(CommandWithJsonObject), command, [], new());
        _discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>()).Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _filter.OnExecution(_context));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    record CommandWithJsonObject(JsonObject Payload);
}
