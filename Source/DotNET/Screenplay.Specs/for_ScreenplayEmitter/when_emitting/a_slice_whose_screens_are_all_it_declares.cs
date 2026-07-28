// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A screen is a body of its own, so a slice whose screens are the only thing it declares is a slice worth keeping -
/// and it is the smallest document a screen can appear in, which makes it the sharpest round trip for one.
/// </summary>
public class a_slice_whose_screens_are_all_it_declares : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with
        {
            Slices =
            [
                .. LibraryApplication.Slices(),
                SliceModel.Empty("Library.Lending.Overview", "Overview", SliceKind.StateView) with
                {
                    Screens =
                    [
                        new("LendingOverview", "Lending/Overview/LendingOverview.tsx"),
                        new("DueSoon", "Lending/Overview/DueSoon.tsx")
                    ]
                }
            ]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    bool Says(string text) => _emission.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_keep_the_slice() => Says("slice StateView Overview").ShouldBeTrue();
    [Fact] void should_declare_every_screen() => Says("screen DueSoon").ShouldBeTrue();
    [Fact] void should_refer_to_the_file_realizing_each_screen() => Says("file Lending/Overview/LendingOverview.tsx").ShouldBeTrue();
    [Fact] void should_order_the_screens_by_name() => _emission.Source.IndexOf("screen DueSoon", StringComparison.Ordinal).ShouldBeLessThan(_emission.Source.IndexOf("screen LendingOverview", StringComparison.Ordinal));
    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_report_nothing_as_unmappable() => _emission.Diagnostics.ShouldBeEmpty();
}
