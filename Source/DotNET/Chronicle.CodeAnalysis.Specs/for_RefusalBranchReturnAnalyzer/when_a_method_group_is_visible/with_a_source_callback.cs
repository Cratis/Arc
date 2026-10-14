// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_a_method_group_is_visible;

public class with_a_source_callback : a_refusal_callback
{
    [Theory]
    [InlineData("=> {|#0:new DoWork()|};")]
    [InlineData("{ return {|#0:new DoWork()|}; }")]
    public async Task should_warn_on_the_callback_return(string body) => await VerifyCS.VerifyAnalyzerAsync(Callback("HandleRefusal", "object HandleRefusal(CommandRefusal refusal) " + body), Warning);
}
