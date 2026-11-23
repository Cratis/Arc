// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandEventSourceIdAnalyzer;

public class when_analyzing_native_union_sources : Specification
{
    [Theory]
    [InlineData("Choice<EventForEventSourceId, ValidationResult>")]
    [InlineData("ValueTask<Choice<EventForEventSourceId, ValidationResult>>")]
    [InlineData("Task<Choice<IEnumerable<EventForEventSourceId>, ValidationResult>>")]
    [InlineData("Choice<(E, EventForEventSourceId), ValidationResult>")]
    [InlineData("Choice<Result<EventForEventSourceId, ValidationResult>, ValidationResult>")]
    public Task should_accept_explicit_sources(string signature) => Verify<CommandEventSourceIdAnalyzer>(Source(signature));

    [Theory]
    [InlineData("Choice<EventForEventSourceId, E>")]
    [InlineData("Choice<ValidationResult, Failure>")]
    [InlineData("Choice<OneOf<EventForEventSourceId, E>, ValidationResult>")]
    public Task should_require_a_source_for_every_event_arm(string signature) => Verify<CommandEventSourceIdAnalyzer>(
        Source(signature), new ExpectedDiagnostic("ARCCHR0002", DiagnosticSeverity.Warning, "First", "Second"));

    static string Source(string signature) => Imports + "\n" + Events + "\n" +
        "[Command] public record Transfer(EventSourceId First, EventSourceId Second) { public " + signature + " Handle() => default; }";
}
#endif
