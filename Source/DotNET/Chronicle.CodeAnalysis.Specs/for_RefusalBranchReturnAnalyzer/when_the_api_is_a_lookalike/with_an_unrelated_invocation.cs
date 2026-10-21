// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.given;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.RefusalBranchReturnAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RefusalBranchReturnAnalyzer.when_the_api_is_a_lookalike;

public class with_an_unrelated_invocation : a_refusal_callback
{
    [Fact]
    public async Task should_not_warn() => await VerifyCS.VerifyAnalyzerAsync(Preamble + @"
        public class CommandInvocation
        {
            public object OnRefused(Refused selector, Func<CommandRefusal, object?> handler) => this;
        }
        public class Reactor
        {
            public object Handle() => new CommandInvocation().OnRefused(Refused.Any, _ => new DoWork());
        }
        ");
}
