// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_acknowledging;

public class with_no_events : a_refusal_callback
{
    [Theory]
    [InlineData("_ => null")]
    [InlineData("_ => { return null; }")]
    [InlineData("_ => default(object)")]
    public async Task should_allow_null(string callback) => await VerifyCS.VerifyAnalyzerAsync(Callback(callback));

    [Fact]
    public async Task should_allow_acknowledge() => await VerifyCS.VerifyAnalyzerAsync(Preamble + "public class Reactor { public CommandInvocation Handle() => Invoke.Command(new DoWork()).Acknowledge(Refused.Any); }");
}
