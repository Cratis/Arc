// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_request_is_cancelled : given.a_command_filter_with_a_gate
{
    Exception _error;

    void Establish()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        _context = new CommandContext(CorrelationId.New(), typeof(given.a_command_filter_with_a_gate.GatedCommand), new given.a_command_filter_with_a_gate.GatedCommand(), [], new CommandContextValues(), null, null, _services, source.Token);
    }

    async Task Because() => _error = await Catch.Exception(() => _filter.OnExecution(_context));

    [Fact] void should_propagate_the_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
}
