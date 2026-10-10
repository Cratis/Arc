// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_an_authoring_command_reads_it : a_routed_model
{
    void Because()
    {
        const string Namespace = "Library.Authors.Registration";
        PropertyModel[] properties = [new("Name", Text), new("Lines", new TypeReferenceModel("Undeclared", false, true))];
        _command = _command with
        {
            Authoring = _command.Authoring! with
            {
                Reads = [new("Report", "report", "Id") { Namespace = Namespace, Properties = properties }],
                Requirements = [new(new ComparisonCondition("report.Name", ComparisonKind.Equal, new LiteralSource("name")), "The report must match")]
            },
            Produces = [new("Registered", null, [new("Name", new PropertyPathSource("Name")), new("PreviousName", new PropertyPathSource("report.Name"))]) { UsesCommandContext = true }]
        };
        var model = Model();
        var slice = model.Slices.Single() with
        {
            Events = [new("Registered", [new("Name", Text), new("PreviousName", Text with { IsOptional = true })], [])],
            ReadModels = [new("Report", properties)],
            Projections = [new("Reports", "Report", "Log", ProjectionAutoMapMode.Enabled, false, ProjectionScopeModel.Empty)]
        };
        _result = _emitter.Emit(model with { Slices = [slice] }, new ScreenplayOptions { AuthoringOnlyConstructs = true });
    }

    [Fact] void should_not_reintroduce_the_read_model_through_reads() => _result.Source.ShouldNotContain("readmodel Report");
    [Fact] void should_leave_out_the_unavailable_read() => _result.Source.ShouldNotContain("reads Report");
    [Fact] void should_leave_out_its_requirement() => _result.Source.ShouldNotContain("The report must match");
    [Fact] void should_leave_out_its_production_mapping() => _result.Source.ShouldNotContain("report.name");
    [Fact] void should_preserve_the_command_route() => _result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_classify_the_withheld_read() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning).Message.ShouldContain("omitted read model");
    [Fact] void should_report_no_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_round_trip_and_bind() => AssertBinds();
}
