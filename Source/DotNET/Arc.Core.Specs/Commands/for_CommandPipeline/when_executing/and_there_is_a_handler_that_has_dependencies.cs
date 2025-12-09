// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_there_is_a_handler_that_has_dependencies : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    object[] _expectedDependencies;
    List<object> _dependencies = [];
    CommandContextValues _expectedValues;
    CommandContext _commandContext;

    void Establish()
    {
        _expectedDependencies = ["Forty two", 42];
        _commandHandler.Dependencies.Returns([typeof(string), typeof(int)]);
        _serviceProvider.GetService(typeof(string)).Returns(_expectedDependencies[0]);
        _serviceProvider.GetService(typeof(int)).Returns(_expectedDependencies[1]);

        _expectedValues = new CommandContextValues
        {
            ["TestKey1"] = "TestValue1",
            ["TestKey2"] = 42,
            ["TestKey3"] = true
        };
        _commandContextValuesBuilder.Build(Arg.Any<object>()).Returns(_expectedValues);

        _commandHandler.When(x => x.Handle(Arg.Any<CommandContext>())).Do(x =>
        {
            _commandContext = x.Arg<CommandContext>();
            _dependencies.AddRange(_commandContext.Dependencies);
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_call_command_handler() => _commandHandler.Received(1).Handle(Arg.Any<CommandContext>());
    [Fact] void should_pass_dependencies_to_handler() => _dependencies.ShouldContainOnly(_expectedDependencies);
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
    [Fact] void should_pass_context_with_values_from_builder() => _commandContext.Values.ShouldEqual(_expectedValues);
}
