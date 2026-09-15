// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.EventTypeIdArgumentAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_EventTypeIdArgumentAnalyzer.when_validating_event_type_attribute;

/// <summary>
/// Proves the id is bound through the resolved constructor's parameter list rather than "whichever positional
/// argument comes first" — with two positional arguments present, position 0 must still resolve to <c>id</c>.
/// </summary>
public class and_the_id_is_the_type_name_with_a_generation : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    [{|#0:EventType(""AuthorRegistered"", 2)|}]
    public record AuthorRegistered(string Name);
}",
                VerifyCS.Diagnostic("ARCCHR0004")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("AuthorRegistered")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
