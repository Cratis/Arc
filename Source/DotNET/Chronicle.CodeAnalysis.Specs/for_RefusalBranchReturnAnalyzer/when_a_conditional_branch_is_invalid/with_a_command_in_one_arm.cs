// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_a_conditional_branch_is_invalid;

public class with_a_command_in_one_arm : a_refusal_callback
{
    [Theory]
    [InlineData("refusal => {|#0:refusal.Message.Length > 0 ? new Recorded() : (object)new DoWork()|}")]
    [InlineData("refusal => { if (refusal.Message.Length > 0) return new Recorded(); return {|#0:new DoWork()|}; }")]
    [InlineData("refusal => {|#0:refusal.Message switch { \"ok\" => new Recorded(), _ => (object)new DoWork() }|}")]
    public async Task should_warn(string callback) => await VerifyCS.VerifyAnalyzerAsync(Callback(callback), Warning);
}
