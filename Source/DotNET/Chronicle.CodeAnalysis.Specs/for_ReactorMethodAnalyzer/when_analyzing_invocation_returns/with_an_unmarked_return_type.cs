// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Chronicle.CodeAnalysis.Analyzers.ReactorMethodAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorMethodAnalyzer.when_analyzing_invocation_returns;

public class with_an_unmarked_return_type
{
    [Fact]
    public async Task should_report_an_unsupported_synchronous_return() => await VerifyCS.VerifyAnalyzerAsync(
        @"
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        [EventType]
        public record BookAdded;
        public record UnknownEffect;

        public class IndexBooks : IReactor
        {
            [OnceOnly]
            public UnknownEffect {|#0:Handle|}(BookAdded @event) => new();
        }
        ",
        VerifyCS.Diagnostic("CHR0054").WithSeverity(DiagnosticSeverity.Warning).WithArguments("Handle", "UnknownEffect"));
}
