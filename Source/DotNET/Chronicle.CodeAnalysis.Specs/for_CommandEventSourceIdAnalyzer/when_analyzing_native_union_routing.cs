// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandEventSourceIdAnalyzer;

public class when_analyzing_native_union_routing : Specification
{
    [Theory]
    [InlineData("(Guid, Choice<E, ValidationResult>)")]
    [InlineData("Choice<(Guid, E), ValidationResult>")]
    [InlineData("ValueTask<Choice<(Guid, E), ValidationResult>>")]
    [InlineData("Choice<OneOf<(Guid, E), Failure>, ValidationResult>")]
    [InlineData("(Guid, Choice<EventForEventSourceId, E>)")]
    [InlineData("(Guid, (int, Choice<E, ValidationResult>))")]
    public Task should_preserve_the_guid_and_bare_event_context(string signature) => Verify<CommandEventSourceIdAnalyzer>(
        Source(signature), new ExpectedDiagnostic("ARCCHR0010", DiagnosticSeverity.Warning, "C", "E"));

    [Theory]
    [InlineData("(Guid, Choice<EventForEventSourceId, ValidationResult>)")]
    [InlineData("(Guid, Choice<int, ValidationResult>)")]
    [InlineData("(EventSourceId, Choice<E, ValidationResult>)")]
    public Task should_not_warn_without_a_fallback_targeted_event(string signature) => Verify<CommandEventSourceIdAnalyzer>(Source(signature));

    static string Source(string signature) => Imports + "\n" + Events + "\n" +
        "[Command] public record C { public " + signature + " Handle() => default; }";
}
#endif
