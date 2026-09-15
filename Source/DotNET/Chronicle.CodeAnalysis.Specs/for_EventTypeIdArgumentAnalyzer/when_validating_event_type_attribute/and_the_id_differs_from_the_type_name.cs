// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.EventTypeIdArgumentAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_EventTypeIdArgumentAnalyzer.when_validating_event_type_attribute;

/// <summary>
/// Pinning an id that differs from the type name is how an event record is renamed without losing the events
/// already stored under the old name: the record becomes <c>FeatureScreenTemplateSet</c> while
/// <c>[EventType("FeatureUITemplateSet")]</c> keeps every stored event resolving under its original identifier.
/// Removing the id here would not be a cleanup — it would orphan every one of those events.
/// </summary>
public class and_the_id_differs_from_the_type_name : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    [EventType(""FeatureUITemplateSet"")]
    public record FeatureScreenTemplateSet(string TemplateId);
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
