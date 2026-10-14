// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_a_batch_has_invalid_members;

public class with_a_mixed_batch : a_refusal_callback
{
    [Theory]
    [InlineData("new object[] { new Recorded(), new DoWork() }")]
    [InlineData("new List<object> { new Recorded(), new DoWork() }")]
    [InlineData("(object[])[new Recorded(), new DoWork()]")]
    [InlineData("new object[] { new Recorded(), null! }")]
    [InlineData("new object[] { new Recorded(), new Recorded[] { new Recorded() } }")]
    public async Task should_warn_without_accepting_a_partial_batch(string value) => await VerifyCS.VerifyAnalyzerAsync(Callback($"_ => {{|#0:{value}|}}"), Warning);
}
