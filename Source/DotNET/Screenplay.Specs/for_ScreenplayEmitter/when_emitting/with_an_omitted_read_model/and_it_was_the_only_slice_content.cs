// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_an_omitted_read_model;

public class and_it_was_the_only_slice_content : an_emitter
{
    ScreenplayEmission _result;

    void Because()
    {
        var model = ApplicationModel.Empty with
        {
            Domain = "Library",
            Slices = [SliceModel.Empty("Library.Authors.Reporting", "Reporting", SliceKind.StateView) with
            {
                ReadModels = [new("Report", [new("Lines", new TypeReferenceModel("Undeclared", false, true))])]
            }]
        };
        _result = _emitter.Emit(model, _options);
    }

    [Fact] void should_leave_out_the_empty_slice() => _result.Source.ShouldNotContain("slice Reporting");
    [Fact] void should_report_the_slice_loss_as_information() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EmptySlice).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_explain_why_the_slice_became_empty() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EmptySlice).Message.ShouldContain("became empty because its read-model dependents were withheld");
    [Fact] void should_report_the_slice_location() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EmptySlice).Location.ShouldEqual("Library.Authors.Reporting");
    [Fact] void should_report_no_warning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
}
