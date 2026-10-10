// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.given;

public class a_stream_id_template : Specification
{
    protected record ScopeId(string Value) : ConceptAs<string>(Value);

    [EventStreamId("{Scope}:{Period}")]
    protected record Composite(ScopeId Scope, DateOnly Period);

    [EventStreamId("constant")]
    protected record Constant;

#pragma warning disable CHR0027 // Intentionally ambiguous to specify Arc's runtime refusal.
    [EventStreamId("{Value}")]
    protected record Ambiguous(string Value) : ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => Value;
    }
#pragma warning restore CHR0027
}
