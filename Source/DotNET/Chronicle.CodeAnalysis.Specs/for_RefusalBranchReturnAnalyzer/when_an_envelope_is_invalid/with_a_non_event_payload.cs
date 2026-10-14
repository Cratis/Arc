// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_an_envelope_is_invalid;

public class with_a_non_event_payload : a_refusal_callback
{
    [Theory]
    [InlineData("new DoWork()")]
    [InlineData("new object()")]
    [InlineData("null!")]
    [InlineData("new EventForEventSourceId(\"id\", new Recorded())")]
    [InlineData("new[] { new Recorded() }")]
    public async Task should_warn(string payload) => await VerifyCS.VerifyAnalyzerAsync(Callback($"_ => {{|#0:new EventForEventSourceId(\"id\", {payload})|}}"), Warning);
}
