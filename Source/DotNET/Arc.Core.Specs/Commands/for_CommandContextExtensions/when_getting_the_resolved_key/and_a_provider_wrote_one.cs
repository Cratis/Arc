// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

public class and_a_provider_wrote_one : Specification
{
    ICommandKeys _commandKeys;
    IServiceProvider _serviceProvider;
    string? _result;

    void Establish()
    {
        _commandKeys = Substitute.For<ICommandKeys>();
        _commandKeys.GetKeyFor(Arg.Any<object>()).Returns("read-from-the-command");
        _serviceProvider = new ServiceCollection().AddSingleton(_commandKeys).BuildServiceProvider();
    }

    void Because() => _result = ContextWith(new CommandContextValues { { CommandContextKeys.ResolvedKey, "written" } }).GetResolvedKey(_serviceProvider);

    [Fact] void should_stand_by_what_was_written() => _result.ShouldEqual("written");
    [Fact] void should_not_read_the_command() => _commandKeys.DidNotReceive().GetKeyFor(Arg.Any<object>());

    static CommandContext ContextWith(CommandContextValues values) =>
        new(CorrelationId.New(), typeof(object), new object(), [], values);
}
