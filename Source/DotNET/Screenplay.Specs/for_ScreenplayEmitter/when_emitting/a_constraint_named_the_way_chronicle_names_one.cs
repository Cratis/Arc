// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// Kebab case is idiomatic for the runtime name of a Chronicle constraint, and a Screenplay identifier cannot carry a
/// hyphen. Deleting the hyphens leaves one run-together word that reads as nothing, so the words either side of each
/// one are joined the way a reader would have written them.
/// </summary>
public class a_constraint_named_the_way_chronicle_names_one : given.an_emitter
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
                SliceModel.Empty("Library.Lending.Reserving.Rules", "Rules", SliceKind.StateChange) with
                {
                    Constraints = [new UniqueEventConstraintModel("unique-book-reservation", "BookReserved")]
                }
            ]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_name_the_constraint_by_the_words_the_source_stated() => _emission.Source.Contains("constraint UniqueBookReservation", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_run_the_words_together() => _emission.Source.Contains("uniquebookreservation", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
}
