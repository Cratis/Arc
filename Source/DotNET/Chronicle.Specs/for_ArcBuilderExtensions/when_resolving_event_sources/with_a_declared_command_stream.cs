// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_event_sources;

[Collection("UsesCurrentDirectory")]
public class with_a_declared_command_stream : given.a_host_with_an_event_source
{
    CommandContextValues _values;

    void Because()
    {
        var provider = ActivatorUtilities.CreateInstance<EventSourceDefinitionValuesProvider>(_scope.ServiceProvider);
        _values = provider.Provide(new RecordEntry(EventSourceId.New(), "Entry"));
    }

    [Fact] void should_resolve_the_event_source_type() => _values[WellKnownCommandContextKeys.EventSourceType].ShouldEqual((EventSourceType)"hosted-ledger");
    [Fact] void should_resolve_the_event_stream_type() => _values[WellKnownCommandContextKeys.EventStreamType].ShouldEqual((EventStreamType)"entries");
    [Fact] void should_resolve_the_event_source_declaration() => _values[WellKnownCommandContextKeys.EventSource].ShouldEqual(typeof(HostedLedger));
    [Fact] void should_resolve_the_event_stream_declaration() => _values[WellKnownCommandContextKeys.EventStream].ShouldEqual("entries");
}
