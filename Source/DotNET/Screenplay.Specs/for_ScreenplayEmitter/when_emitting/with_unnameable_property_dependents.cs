// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

public class with_unnameable_property_dependents : given.a_routed_model
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_dependent_claims_in_both_modes(bool authoring)
    {
        var unknown = new TypeReferenceModel("IRule", false, true) { UnnameableType = "Library.IRule?" };
        var report = new TypeReferenceModel("Report", true, false);
        var slice = SliceModel.Empty("Library.Authors.Reporting", "Reporting", SliceKind.StateView) with
        {
            ReadModels = [new("Report", [new("ReportId", Text), new("Rule", unknown)])],
            Queries =
            [
                new("AllReports", report, null, [], null),
                new("ReportByRule", report with { IsCollection = false }, new("Rule", unknown), [], null)
            ],
            Screens = [new("Reports", "Reports.tsx")
            {
                Data = [new("AllReports", report, null)],
                Tables = [new("Report", [new("Rule", "Rule")])],
                Titles = ["Reports"]
            }],
            Specifications = [new("a_rule_is_stated", [], null, [new("Report", SpecificationStateKind.ReadModel, [new("Rule", new LiteralSource(null))])], [])]
        };
        _result = _emitter.Emit(ApplicationModel.Empty with { Domain = "Library", Module = "Library", Slices = [slice] },
            _options with { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = SemanticVersion.V28 });

        _result.Source.ShouldContain("readmodel Report");
        _result.Source.ShouldContain("query AllReports");
        _result.Source.ShouldNotContain("query ReportByRule");
        _result.Source.ShouldNotContain("data Report");
        _result.Source.ShouldNotContain("table Report");
        _result.Source.ShouldNotContain("specification ");
        _result.Source.ShouldContain("title \"Reports\"");
        _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        AssertBinds();
    }
}
