// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

/// <summary>
/// An empty key is the verdict of an integration that owns key resolution — Chronicle writes one when the command
/// carried nothing usable — so reading the command behind its back would overturn it.
/// </summary>
public class and_a_provider_wrote_an_empty_one : Specification
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

    void Because() => _result = ContextWith(new CommandContextValues { { CommandContextKeys.ResolvedKey, string.Empty } }).GetResolvedKey(_serviceProvider);

    [Fact] void should_stand_by_the_empty_key() => _result.ShouldEqual(string.Empty);
    [Fact] void should_not_read_the_command() => _commandKeys.DidNotReceive().GetKeyFor(Arg.Any<object>());

    static CommandContext ContextWith(CommandContextValues values) =>
        new(CorrelationId.New(), typeof(object), new object(), [], values);
}
