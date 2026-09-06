// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.EventTypeIdArgumentAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_EventTypeIdArgumentAnalyzer.when_validating_event_type_attribute;

public class and_the_id_is_a_constant_that_differs : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    public static class EventIds
    {
        public const string FeatureUITemplateSet = ""FeatureUITemplateSet"";
    }

    [EventType(EventIds.FeatureUITemplateSet)]
    public record FeatureScreenTemplateSet(string TemplateId);
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
