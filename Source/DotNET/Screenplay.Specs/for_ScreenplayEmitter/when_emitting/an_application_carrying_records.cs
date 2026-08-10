// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A record an artifact carries is a shape the document declares in its own right, so the property naming it resolves
/// to a declaration rather than to nothing. A name is what two shapes finally collide under, and it is emission that
/// decides what a name becomes - so a shape whose name a concept was already written under is the one that has to be
/// left out here, because a document declaring one word twice does not compile.
/// </summary>
public class an_application_carrying_records : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with
        {
            Types =
            [
                new("ShelfPosition", [new("Aisle", new("CopyCount", false, false)), new("Note", new("String", false, true))]),
                new("BookTitle", [new("Value", new("String", false, false))])
            ],
            Slices =
            [
                .. LibraryApplication.Slices(),
                SliceModel.Empty("Library.Inventory.Shelving", "Shelving", SliceKind.StateChange) with
                {
                    Events = [new("BookShelved", [new("Position", new("ShelfPosition", false, false))], [])]
                }
            ]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_declare_the_shape() => _emission.Source.Contains("type ShelfPosition", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_what_the_shape_carries() => _emission.Source.Contains("  aisle CopyCount", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_a_value_may_be_absent() => _emission.Source.Contains("  note String?", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_out_a_shape_a_concept_is_already_declared_as() => _emission.Source.Contains("type BookTitle", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_say_which_shape_it_left_out() => _emission.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableShape).Message.Contains("BookTitle", StringComparison.Ordinal).ShouldBeTrue();
}
