// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_an_empty_collection_is_returned;

public class with_a_literal_empty_batch : a_refusal_callback
{
    [Theory]
    [InlineData("new object[] { }")]
    [InlineData("new Recorded[0]")]
    [InlineData("Array.Empty<Recorded>()")]
    [InlineData("new List<Recorded>()")]
    [InlineData("(object[])[]")]
    public async Task should_warn(string value) => await VerifyCS.VerifyAnalyzerAsync(Callback($"_ => {{|#0:{value}|}}"), Warning);
}
