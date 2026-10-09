// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer.given.nullable_event_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.NullableCommandEventReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_NullableCommandEventReturnAnalyzer;

public class when_stream_elements_are_returned_beside_events
{
    const string Stubs = "\nnamespace Cratis.Arc.Chronicle.Streams { public sealed record CompleteStream; }";

    [Theory]
    [InlineData("(E, Cratis.Arc.Chronicle.Streams.CompleteStream)", "=> (new(), new());")]
    [InlineData("(E, Cratis.Arc.Chronicle.Streams.CompleteStream, Cratis.Arc.Chronicle.Commands.EventTags)", "=> default!;")]
    [InlineData("(E, Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes)", "=> default!;")]
    [InlineData("Task<(E, Cratis.Arc.Chronicle.Streams.CompleteStream)>", "=> default!;")]
    [InlineData("Result<(E, Cratis.Arc.Chronicle.Streams.CompleteStream), ValidationResult>", "=> default!;")]
    [InlineData("Cratis.Arc.Chronicle.Streams.CompleteStream", "=> new();")]
    [InlineData("Cratis.Arc.Chronicle.Commands.EventTags", "=> default!;")]
    [InlineData("Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes", "=> default!;")]
    public async Task should_not_report_a_diagnostic(string signature, string body) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(signature, body) + Stubs);
}
