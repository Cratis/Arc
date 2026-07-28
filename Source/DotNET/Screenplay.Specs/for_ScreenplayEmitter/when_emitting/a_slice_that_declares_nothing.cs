// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A slice header with no body does not compile, so a slice that declares nothing has to be left out. Leaving it out
/// silently is what makes generated output impossible to trust, so the drop is reported.
/// </summary>
public class a_slice_that_declares_nothing : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with
        {
            Slices = [.. LibraryApplication.Slices(), SliceModel.Empty("Library.Lending.Archiving", "Archiving", SliceKind.StateView)]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_leave_the_slice_out() => _emission.Source.Contains("Archiving", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_slice_as_dropped() => _emission.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.EmptySlice]);
    [Fact] void should_locate_the_report_at_the_slice() => _emission.Diagnostics.Single().Location.ShouldEqual("Library.Lending.Archiving");
    [Fact] void should_report_it_as_a_warning() => _emission.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_still_compile() => _roundTrip.Errors.ShouldBeEmpty();
}
