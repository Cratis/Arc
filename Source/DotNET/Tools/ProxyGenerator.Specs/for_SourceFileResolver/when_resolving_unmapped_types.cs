// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_SourceFileResolver;

public class when_resolving_unmapped_types : Specification
{
    Dictionary<string, string> _result = null!;

    void Establish()
    {
        _result = new Dictionary<string, string>
        {
            ["MyApp.History.ConsultantHistory"] = "ConsultantHistory",
            ["MyApp.History.ConsultantHistoryProjection"] = "ConsultantHistory"
        };
    }

    void Because() => SourceFileResolver.ResolveUnmappedTypes(
        _result,
        [
            ("MyApp.History.EvaluationOutcome", "MyApp.History"),
            ("MyApp.History.MissionOutcome", "MyApp.History")
        ]);

    [Fact] void should_resolve_first_enum_to_source_file() =>
        _result["MyApp.History.EvaluationOutcome"].ShouldEqual("ConsultantHistory");

    [Fact] void should_resolve_second_enum_to_source_file() =>
        _result["MyApp.History.MissionOutcome"].ShouldEqual("ConsultantHistory");
}
