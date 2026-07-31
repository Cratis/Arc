// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class and_nothing_can_read_a_key_from_the_command : given.a_command_context_values_builder
{
    CommandContextValues _result;

    void Establish()
    {
        _providers.GetEnumerator().Returns(_ => new List<ICommandContextValuesProvider>().GetEnumerator());
        _commandKeys.GetKeyFor(Arg.Any<object>()).Returns((string?)null);
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_leave_the_key_out_rather_than_hold_an_empty_one() => _result.ContainsKey(CommandContextKeys.ResolvedKey).ShouldBeFalse();
}
