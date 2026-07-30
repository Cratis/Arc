// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

public class and_nothing_can_read_the_command : Specification
{
    IServiceProvider _serviceProvider;
    string? _result;

    void Establish() => _serviceProvider = new ServiceCollection().BuildServiceProvider();

    void Because() => _result = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues()).GetResolvedKey(_serviceProvider);

    [Fact] void should_resolve_nothing() => _result.ShouldBeNull();
}
