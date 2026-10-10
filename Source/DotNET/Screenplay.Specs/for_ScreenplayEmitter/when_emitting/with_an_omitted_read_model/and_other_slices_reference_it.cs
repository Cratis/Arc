// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_other_slices_reference_it : a_routed_model
{
    void Because()
    {
        var model = Model();
        var report = new TypeReferenceModel("Report", false, true);
        var owner = model.Slices.Single() with
        {
            ReadModels = [new("Report", [new("Lines", new TypeReferenceModel("Undeclared", false, true))])],
            Queries = [new("AllReports", report, null, [], null)]
        };
        var consumer = SliceModel.Empty("Library.Authors.Reporting", "Reporting", SliceKind.StateView) with
        {
            Queries = [new("OtherReports", report, null, [], null)],
            Screens = [new("Reports", "Reports.tsx")
            {
                Data = [new("OtherReports", report, null)],
                Tables = [new("Report", [new("Lines", "Lines")])],
                Titles = ["Reports"]
            }],
            Projections = [new("ReportProjection", "Report", "Log", ProjectionAutoMapMode.Enabled, false, ProjectionScopeModel.Empty)],
            Specifications = [new("reports_are_built", [], null, [new("Report", SpecificationStateKind.ReadModel, [])], [])]
        };
        _result = _emitter.Emit(model with { Slices = [owner, consumer] }, _options);
    }

    [Fact] void should_leave_out_every_query_returning_the_model() => _result.Source.ShouldNotContain("query");
    [Fact] void should_leave_out_screen_data_reading_the_model() => _result.Source.ShouldNotContain("data Report");
    [Fact] void should_leave_out_tables_naming_the_model() => _result.Source.ShouldNotContain("table Report");
    [Fact] void should_preserve_the_screen_and_its_unaffected_title() => _result.Source.ShouldContain("title \"Reports\"");
    [Fact] void should_leave_out_the_projection() => _result.Source.ShouldNotContain("projection");
    [Fact] void should_leave_out_the_dependent_scenario() => _result.Source.ShouldNotContain("reports_are_built");
    [Fact] void should_report_each_dependent_reference() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference).ShouldEqual(6);
    [Fact] void should_report_no_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_round_trip_and_bind() => AssertBinds();
}
