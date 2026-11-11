// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_handle_returns_a_one_of_with_explicit_sources
{
    [Theory]
    [InlineData("OneOf<EventForEventSourceId, ValidationResult>")]
    [InlineData("OneOf<ValidationResult, IEnumerable<EventForEventSourceId>>")]
    [InlineData("OneOf<EventForEventSourceId, IEnumerable<EventForEventSourceId>>")]
    [InlineData("OneOf<(Fact, EventForEventSourceId), ValidationResult>")]
    [InlineData("Task<OneOf<EventForEventSourceId, ValidationResult>>")]
    [InlineData("OneOf<Result<EventForEventSourceId, ValidationResult>, ValidationResult>")]
    public Task should_accept_when_every_arm_is_explicit_or_a_validation_result(string responseType) => AnalyzerVerifier<CommandEventSourceIdAnalyzer>.VerifyAnalyzerAsync(Source(responseType));

    [Theory]
    [InlineData("OneOf<EventForEventSourceId, Fact>")]
    [InlineData("OneOf<Fact, ValidationResult>")]
    [InlineData("OneOf<ValidationResult, ValidationResult>")]
    [InlineData("Task<OneOf<EventForEventSourceId, Fact>>")]
    [InlineData("OneOf<EventForEventSourceId, ValidationResult, Fact>")]
    [InlineData("OneOf<Result<Fact, ValidationResult>, EventForEventSourceId>")]
    public Task should_still_report_when_an_arm_is_not_an_event_source(string responseType) => AnalyzerVerifier<CommandEventSourceIdAnalyzer>.VerifyAnalyzerAsync(
        Source(responseType), new ExpectedDiagnostic("ARCCHR0002", DiagnosticSeverity.Warning, "First", "Second"));

    static string Source(string responseType) => $$"""
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Monads;
        using OneOf;
        public record Fact;
        [Command]
        public record Transfer(EventSourceId First, EventSourceId Second)
        {
            public {{responseType}} Handle() => default!;
        }
        """;
}
