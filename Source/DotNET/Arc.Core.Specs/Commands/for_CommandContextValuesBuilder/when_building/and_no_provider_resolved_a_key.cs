// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class and_no_provider_resolved_a_key : given.a_command_context_values_builder
{
    CommandContextValues _result;

    void Establish()
    {
        _providers.GetEnumerator().Returns(_ => new List<ICommandContextValuesProvider>().GetEnumerator());
        _commandKeys.GetKeyFor(_command).Returns("read-from-the-command");
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_read_the_key_from_the_command() => _result[CommandContextKeys.ResolvedKey].ShouldEqual("read-from-the-command");
}
