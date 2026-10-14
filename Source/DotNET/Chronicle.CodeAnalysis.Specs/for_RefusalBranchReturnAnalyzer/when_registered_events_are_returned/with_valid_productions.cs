// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_registered_events_are_returned;

public class with_valid_productions : a_refusal_callback
{
    [Theory]
    [InlineData("new Recorded()")]
    [InlineData("new EventForEventSourceId(\"id\", new Recorded())")]
    [InlineData("new[] { new Recorded(), new Recorded() }")]
    [InlineData("new List<Recorded> { new Recorded() }")]
    [InlineData("new object[] { new Recorded(), new EventForEventSourceId(\"id\", new Recorded()) }")]
    [InlineData("(object[])[new Recorded()]")]
    [InlineData("(IEnumerable<Recorded>)[new Recorded()]")]
    [InlineData("(IReadOnlyList<Recorded>)[new Recorded()]")]
    public async Task should_allow_events_and_batches(string value) => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => " + value));

    [Fact]
    public async Task should_allow_an_inherited_event_attribute() => await VerifyCS.VerifyAnalyzerAsync(Callback("_ => new Derived()") + "public record Derived : Recorded;");

    [Fact]
    public async Task should_allow_a_visible_event_method_group() => await VerifyCS.VerifyAnalyzerAsync(Callback("HandleRefusal", "Recorded HandleRefusal(CommandRefusal refusal) => new();"));
}
