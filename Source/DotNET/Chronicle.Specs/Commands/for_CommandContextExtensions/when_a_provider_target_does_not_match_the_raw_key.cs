// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions;

public class when_a_provider_target_does_not_match_the_raw_key : Specification
{
    CommandContext _context;
    object? _raw;

    void Establish()
    {
        var command = new ProviderCommand("*");
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
        _context = new(CorrelationId.NotSet, command.GetType(), command, [], values);
    }

    void Because() => _raw = _context.GetRawEventSourceKey();
    [Fact] void should_keep_the_provider_target() => _raw.ShouldEqual((EventSourceId)"provider-target");
    [Fact] void should_not_recanonicalize_the_provider_target() => _context.GetEventSourceId().Value.ShouldEqual("provider-target");

    record ProviderCommand([Key] string Id) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => "provider-target";
    }
}
