// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

/// <summary>
/// An integration that owns key resolution has the last word on the command's key, so the command is not read again
/// behind it.
/// </summary>
public class and_a_provider_resolved_a_key : given.a_command_context_values_builder
{
    CommandContextValues _result;

    void Establish()
    {
        var provider = Substitute.For<ICommandContextValuesProvider>();
        provider.Provide(Arg.Any<object>()).Returns(new CommandContextValues { { CommandContextKeys.ResolvedKey, "from-the-provider" } });
        _providers.GetEnumerator().Returns(_ => new List<ICommandContextValuesProvider> { provider }.GetEnumerator());
        _commandKeys.GetKeyFor(Arg.Any<object>()).Returns("read-from-the-command");
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_keep_the_key_the_provider_resolved() => _result[CommandContextKeys.ResolvedKey].ShouldEqual("from-the-provider");
    [Fact] void should_not_read_the_command() => _commandKeys.DidNotReceive().GetKeyFor(Arg.Any<object>());
}
