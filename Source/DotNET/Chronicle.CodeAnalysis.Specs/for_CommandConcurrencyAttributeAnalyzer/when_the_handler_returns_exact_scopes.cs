// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture intentionally concatenates raw source snippets.
#pragma warning disable SA1117 // Snippet arguments and expected diagnostics are displayed separately.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandConcurrencyAttributeAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer;

public class when_the_handler_returns_exact_scopes
{
    [Theory]
    [InlineData("EventsWithConcurrencyScopes")]
    [InlineData("Task<EventsWithConcurrencyScopes>")]
    [InlineData("ValueTask<EventsWithConcurrencyScopes>")]
    [InlineData("(EventsWithConcurrencyScopes, string)")]
    [InlineData("Task<(string, EventsWithConcurrencyScopes)>")]
    public async Task should_report_the_flag_for_every_return_shape(string signature) =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId(\"fixed\", {|#0:concurrency: true|})]", signature), Warning());

    [Theory]
    [InlineData("[EventStreamId(\"fixed\", {|#0:true|})]", "EventStreamId")]
    [InlineData("[EventStreamId({|#0:concurrency: true|})]", "EventStreamId")]
    [InlineData("[EventStreamType(\"Stream\", {|#0:concurrency: true|})]", "EventStreamType")]
    [InlineData("[EventStreamType(\"Stream\", {|#0:true|})]", "EventStreamType")]
    [InlineData("[EventSourceType(\"Source\", {|#0:concurrency: true|})]", "EventSourceType")]
    public async Task should_report_a_positional_or_named_flag(string attribute, string name) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(attribute, "EventsWithConcurrencyScopes"), Warning(name));

    [Fact]
    public async Task should_report_a_named_flag_that_comes_first() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId({|#0:concurrency: true|}, value: \"x\")]", "EventsWithConcurrencyScopes"), Warning());

    [Fact]
    public async Task should_report_each_flagged_attribute() =>
        await VerifyCS.VerifyAnalyzerAsync(
            Command("[EventSourceType(\"S\", concurrency: true)]\n[EventStreamType(\"T\", concurrency: true)]\n[EventStreamId(\"i\", concurrency: true)]", "EventsWithConcurrencyScopes"),
            Warning("EventSourceType"), Warning("EventStreamType"), Warning("EventStreamId"));
}
