// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_no_gate_is_registered : Specification
{
    CommandResult _result;

    async Task Because()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new CommandContext(CorrelationId.New(), typeof(Command), new Command(), [], new CommandContextValues(), null, null, services);
        _result = await new AvailabilityFilter().OnExecution(context);
    }

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    public record Command;
}
