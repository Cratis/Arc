// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_handle_returns_stream_elements_beside_events : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Streams
{
    public sealed record CompleteStream(EventStreamType? EventStreamType = default, EventStreamId? EventStreamId = default);
}

namespace TestNamespace
{
    [EventType]
    public record AuthorRegistered(string Name);

    [Command]
    public record RegisterAuthor(Guid Id, string Name) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => Id;
        public (AuthorRegistered, CompleteStream) First() => (new(Name), new());
        public (AuthorRegistered, CompleteStream, EventTags) Handle() => (new(Name), new(), new EventTags([]));
        public Task<(AuthorRegistered, CompleteStream)> Second() => Task.FromResult((new AuthorRegistered(Name), new CompleteStream()));
        public (EventsWithConcurrencyScopes, CompleteStream, EventTags) Third() => default!;
    }
}"));

    [Fact] void should_not_report_a_diagnostic() => _result.ShouldBeNull();
}
