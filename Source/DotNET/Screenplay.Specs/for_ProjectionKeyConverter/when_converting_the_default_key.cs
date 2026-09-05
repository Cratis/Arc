// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Projections;

namespace Cratis.Arc.Screenplay.for_ProjectionKeyConverter;

/// <summary>
/// The event source id is the implicit key of every block, so writing it out would add noise without adding meaning.
/// A key that is merely the default has to be told apart from one that was lost, because only the latter is worth
/// reporting.
/// </summary>
public class when_converting_the_default_key : Specification
{
    [Fact] void should_treat_the_event_source_id_as_the_default() => ProjectionKeyConverter.IsDefault("$eventSourceId").ShouldBeTrue();
    [Fact] void should_treat_no_key_as_the_default() => ProjectionKeyConverter.IsDefault(null).ShouldBeTrue();
    [Fact] void should_not_treat_an_unmappable_key_as_the_default() => ProjectionKeyConverter.IsDefault("$unknownExpression").ShouldBeFalse();
    [Fact] void should_emit_no_key_for_the_event_source_id() => ProjectionKeyConverter.Convert("$eventSourceId", "Order").ShouldBeNull();
}
