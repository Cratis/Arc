// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

public class and_nothing_wrote_one : Specification
{
    IServiceProvider _serviceProvider;
    object _command;
    string? _result;

    void Establish()
    {
        _command = new object();
        var commandKeys = Substitute.For<ICommandKeys>();
        commandKeys.GetKeyFor(_command).Returns("read-from-the-command");
        _serviceProvider = new ServiceCollection().AddSingleton(commandKeys).BuildServiceProvider();
    }

    void Because() => _result = new CommandContext(CorrelationId.New(), typeof(object), _command, [], new CommandContextValues()).GetResolvedKey(_serviceProvider);

    [Fact] void should_read_the_key_from_the_command() => _result.ShouldEqual("read-from-the-command");
}
