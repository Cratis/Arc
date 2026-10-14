// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Chronicle.CodeAnalysis.Analyzers.ReactorMethodAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorMethodAnalyzer.when_analyzing_invocation_returns;

public class with_supported_invocations
{
    [Theory]
    [InlineData("CommandInvocation")]
    [InlineData("CommandInvocation[]")]
    [InlineData("IEnumerable<CommandInvocation>")]
    [InlineData("List<CommandInvocation>")]
    [InlineData("Task<CommandInvocation>")]
    public async Task should_accept_the_declared_return_type(string returnType) => await VerifyCS.VerifyAnalyzerAsync($@"
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Reactors;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        [EventType]
        public record BookAdded;

        public class IndexBooks : IReactor
        {{
            [OnceOnly]
            public {returnType} Handle(BookAdded @event) => default!;
        }}
        ");
}
