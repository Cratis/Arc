// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_handle_returns_only_event_for_event_source_id : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    public record AuthorRegistered(string Name);

    [Command]
    public record RegisterAuthor(EventSourceId First, EventSourceId Second)
    {
        public EventForEventSourceId Handle() => new(First, new AuthorRegistered(""John""));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
