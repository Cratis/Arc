// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_UniqueConstraint.when_executing_command;

public class and_the_unique_event_type_already_exists : given.a_command_that_appends_an_event_of_a_unique_event_type
{
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new ACommandWithUniqueEventTypeEvent
    {
        EventSourceId = _eventSourceId,
        Name = "Bob"
    });

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
