// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_command_carries_no_gate : given.a_command_filter_with_a_gate
{
    void Establish() => _context = new CommandContext(CorrelationId.New(), typeof(Ungated), new Ungated(), [], new CommandContextValues(), null, null, _services);

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_decide() => _gate.DidNotReceiveWithAnyArgs().Decide(default!, default!, Arg.Any<CancellationToken>());

    public record Ungated;
}
