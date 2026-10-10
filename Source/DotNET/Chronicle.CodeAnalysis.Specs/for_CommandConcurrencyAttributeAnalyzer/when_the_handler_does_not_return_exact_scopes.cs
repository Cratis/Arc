// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandConcurrencyAttributeAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer;

public class when_the_handler_does_not_return_exact_scopes
{
    [Theory]
    [InlineData("E")]
    [InlineData("Task<E>")]
    [InlineData("ValueTask<E>")]
    [InlineData("(E, string)")]
    [InlineData("object[]")]
    [InlineData("EventForEventSourceId")]
    [InlineData("string")]
    public async Task should_not_report_the_flag_for_other_return_shapes(string signature) =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId(\"fixed\", concurrency: true)]", signature));

    [Theory]
    [InlineData("[EventStreamId(\"fixed\", concurrency: false)]")]
    [InlineData("[EventStreamId(\"fixed\", false)]")]
    [InlineData("[EventStreamId(\"fixed\")]")]
    [InlineData("[EventStreamId]")]
    [InlineData("[EventStreamType(\"Stream\")]")]
    [InlineData("[EventSourceType(\"Source\")]")]
    public async Task should_not_report_when_the_flag_is_not_true(string attribute) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(attribute, "EventsWithConcurrencyScopes"));

    [Fact]
    public async Task should_not_report_an_event_source_definition() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventSource<Definition>]", "EventsWithConcurrencyScopes"));

    [Fact]
    public async Task should_ignore_types_that_are_not_commands() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "\n[EventStreamId(\"x\", concurrency: true)] public record C { public EventsWithConcurrencyScopes Handle() => default!; }");
}
