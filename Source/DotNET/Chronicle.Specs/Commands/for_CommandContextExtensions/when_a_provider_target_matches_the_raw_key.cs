// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions;

public class when_a_provider_target_matches_the_raw_key : Specification
{
    CommandContext _context;
    Composite _key;
    object? _raw;

    void Establish()
    {
        _key = new("*");
        var command = new ProviderCommand(_key);
        var values = new EventSourceValuesProvider(NullLogger<EventSourceValuesProvider>.Instance).Provide(command);
        _context = new(CorrelationId.NotSet, command.GetType(), command, [], values);
    }

    void Because() => _raw = _context.GetRawEventSourceKey();
    [Fact] void should_recover_the_original_parts_from_the_existing_selection_site() => _raw.ShouldBeSame(_key);

    record ProviderCommand([Key] Composite Id) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => "provider-target";
    }

    record Composite(string Part)
    {
        public override string ToString() => "provider-target";
    }
}
