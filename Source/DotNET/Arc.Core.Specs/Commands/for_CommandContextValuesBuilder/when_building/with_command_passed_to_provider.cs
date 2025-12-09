// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_command_passed_to_provider : given.a_command_context_values_builder
{
    CommandContextValues _result;
    ICommandContextValuesProvider _provider;
    object _passedCommand;

    void Establish()
    {
        _provider = Substitute.For<ICommandContextValuesProvider>();
        _provider.Provide(Arg.Do<object>(cmd => _passedCommand = cmd)).Returns(new CommandContextValues());
        _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider> { _provider }.GetEnumerator());
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_pass_command_to_provider() => _passedCommand.ShouldEqual(_command);
    [Fact] void should_call_provide_with_command() => _provider.Received(1).Provide(_command);
}