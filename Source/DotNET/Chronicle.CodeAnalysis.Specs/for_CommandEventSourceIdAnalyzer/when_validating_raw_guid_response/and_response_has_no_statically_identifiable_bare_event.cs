// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_response_has_no_statically_identifiable_bare_event
{
    [Theory]
    [InlineData("(Guid, EventForEventSourceId)", "=> (Guid.NewGuid(), new EventForEventSourceId(EventSourceId.New(), new E()));")]
    [InlineData("(Guid, EventForEventSourceId[])", "=> (Guid.NewGuid(), new[] { new EventForEventSourceId(EventSourceId.New(), new E()) });")]
    [InlineData("(Guid, OneOf<EventForEventSourceId, Failure>)", "=> (Guid.NewGuid(), new EventForEventSourceId(EventSourceId.New(), new E()));")]
    [InlineData("(Guid, IEnumerable<object>)", "=> (Guid.NewGuid(), new object[] { new E() });")]
    [InlineData("(Guid, object)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(Guid, Holder<E>)", "=> (Guid.NewGuid(), new Holder<E>());")]
    [InlineData("Other.Task<(Guid, E)>", "=> new();")]
    [InlineData("Other.ValueTask<(Guid, E)>", "=> new();")]
    [InlineData("Other.Result<(Guid, E), Failure>", "=> new();")]
    [InlineData("Other.OneOf<(Guid, E), Failure>", "=> new();")]
    [InlineData("(Guid, Other.Event)", "=> (Guid.NewGuid(), new Other.Event());")]
    [InlineData("(Guid, Other.InheritedEvent)", "=> (Guid.NewGuid(), new Other.InheritedEvent());")]
    [InlineData("(Guid, IEnumerable<Other.InheritedEvent>)", "=> (Guid.NewGuid(), new Other.InheritedEvent[] { new() });")]
    [InlineData("(Guid, ValueEvent[])", "=> (Guid.NewGuid(), new ValueEvent[0]);")]
    public async Task should_not_claim_an_erased_or_explicitly_targeted_value_is_a_bare_event(string signature, string body) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(signature, body) + @"
            public record Holder<T>;
            public record struct ValueEvent;
            namespace Other
            {
                public record Task<T>;
                public record ValueTask<T>;
                public record Result<T, TError>;
                public record OneOf<T, TError>;
                public class EventTypeAttribute : Attribute;
                [EventType] public record Event;
                public record InheritedEvent : Event;
            }
            ");
}
