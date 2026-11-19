// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter.given;

public class a_command_filter_with_a_gate : Specification
{
    protected IAvailabilityGate _gate;
    protected AvailabilityFilter _filter;
    protected ServiceProvider _services;
    protected CommandContext _context;
    protected CommandResult _result;

    void Establish()
    {
        _gate = Substitute.For<IAvailabilityGate>();
        _gate.IsGated(typeof(GatedCommand)).Returns(true);
        _services = new ServiceCollection().AddSingleton(_gate).BuildServiceProvider();
        _filter = new AvailabilityFilter();
        _context = new CommandContext(CorrelationId.New(), typeof(GatedCommand), new GatedCommand(), [], new CommandContextValues(), null, null, _services);
    }

    void Destroy() => _services.Dispose();

    public record GatedCommand;
}
