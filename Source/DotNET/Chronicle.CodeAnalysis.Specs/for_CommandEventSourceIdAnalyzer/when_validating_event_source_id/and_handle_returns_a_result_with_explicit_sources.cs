// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_handle_returns_a_result_with_explicit_sources
{
    [Theory]
    [InlineData("EventForEventSourceId")]
    [InlineData("IEnumerable<EventForEventSourceId>")]
    public Task should_accept_the_success_branch(string successType) => AnalyzerVerifier<CommandEventSourceIdAnalyzer>.VerifyAnalyzerAsync(Source($"Result<{successType}, ValidationResult>"));

    [Theory]
    [InlineData("Result<EventForEventSourceId, ValidationResult>")]
    [InlineData("Result<IEnumerable<EventForEventSourceId>, ValidationResult>")]
    public Task should_accept_an_async_result(string resultType) => AnalyzerVerifier<CommandEventSourceIdAnalyzer>.VerifyAnalyzerAsync(Source($"Task<{resultType}>"));

    [Theory]
    [InlineData("Result<Fact, ValidationResult>")]
    [InlineData("Task<Result<Fact, EventForEventSourceId>>")]
    public Task should_still_report_an_untargeted_success_branch(string responseType) => AnalyzerVerifier<CommandEventSourceIdAnalyzer>.VerifyAnalyzerAsync(
        Source(responseType), new ExpectedDiagnostic("ARCCHR0002", DiagnosticSeverity.Warning, "First", "Second"));

    static string Source(string responseType) => $$"""
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Monads;
        public record Fact;
        [Command]
        public record Transfer(EventSourceId First, EventSourceId Second)
        {
            public {{responseType}} Handle() => default!;
        }
        """;
}
