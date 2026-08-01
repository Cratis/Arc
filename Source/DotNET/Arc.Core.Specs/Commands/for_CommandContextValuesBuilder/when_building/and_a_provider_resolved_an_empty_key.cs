// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

/// <summary>
/// An empty key is a verdict, not an absence — Chronicle writes one when the command carried nothing usable — so
/// reading the command behind it would resolve a read model the integration said there was no key for.
/// </summary>
public class and_a_provider_resolved_an_empty_key : given.a_command_context_values_builder
{
    CommandContextValues _result;

    void Establish()
    {
        var provider = Substitute.For<ICommandContextValuesProvider>();
        provider.Provide(Arg.Any<object>()).Returns(new CommandContextValues { { CommandContextKeys.ResolvedKey, string.Empty } });
        _providers.GetEnumerator().Returns(_ => new List<ICommandContextValuesProvider> { provider }.GetEnumerator());
        _commandKeys.GetKeyFor(Arg.Any<object>()).Returns("read-from-the-command");
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_keep_the_empty_key() => _result[CommandContextKeys.ResolvedKey].ShouldEqual(string.Empty);
    [Fact] void should_not_read_the_command() => _commandKeys.DidNotReceive().GetKeyFor(Arg.Any<object>());
}
