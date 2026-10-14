// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_a_command_is_returned;

public class with_a_constructed_command : a_refusal_callback
{
    [Theory]
    [InlineData("_ => {|#0:new DoWork()|}")]
    [InlineData("_ => { return {|#0:new DoWork()|}; }")]
    [InlineData("delegate(CommandRefusal refusal) { return {|#0:new DoWork()|}; }")]
    [InlineData("_ => {|#0:(object)new DoWork()|}")]
    public async Task should_warn(string callback) => await VerifyCS.VerifyAnalyzerAsync(Callback(callback), Warning);
}
