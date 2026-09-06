// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.EventTypeIdArgumentAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_EventTypeIdArgumentAnalyzer.when_validating_event_type_attribute;

/// <summary>
/// Documents that the comparison is ordinal: Chronicle's <c>EventTypeId</c> is compared ordinally, so an id that
/// differs only in casing from the type name is a genuinely different identifier, not a redundant one.
/// </summary>
public class and_the_id_differs_only_in_casing : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    [EventType(""authorregistered"")]
    public record AuthorRegistered(string Name);
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
