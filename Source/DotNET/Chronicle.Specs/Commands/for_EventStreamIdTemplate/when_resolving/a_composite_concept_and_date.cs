// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class a_composite_concept_and_date : given.a_stream_id_template
{
    Composite _command;
    EventStreamId _result;

    void Establish() => _command = new(new("reporting"), new(2026, 10, 1));
    void Because() => _result = EventStreamIdTemplate.ResolveFor(_command)!;

    [Fact] void should_format_the_underlying_concept_and_date() => _result.Value.ShouldEqual("reporting:2026-10-01");
    [Fact] void should_match_the_pipeline_values() => new EventStreamIdValuesProvider().Provide(_command)[WellKnownCommandContextKeys.EventStreamId].ShouldEqual(_result);
    [Fact] void should_list_both_properties() => EventStreamIdTemplate.PropertiesOf("{Scope}:{Period}").ShouldEqual("Scope", "Period");
}
