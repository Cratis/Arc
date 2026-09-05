// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A scenario about a read model issues no command - the events are what happened, and what followed is the model
/// they built. The language holds that as a specification with no <c>when</c> at all rather than as one naming an
/// empty command, so what comes out has to be the first of those and not the second.
/// </summary>
public class a_scenario_that_issues_no_command : given.an_emitter
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
                SliceModel.Empty("Library.Authors.Watching", "Watching", SliceKind.StateView) with
                {
                    Events = [new("AuthorFollowed", [], [])],
                    Specifications =
                    [
                        new(
                            "WhenAnAuthorIsFollowed",
                            [new("AuthorFollowed", SpecificationStateKind.Event, [])],
                            null,
                            [new("AuthorWatch", SpecificationStateKind.ReadModel, [])],
                            [])
                    ]
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
    [Fact] void should_state_what_had_happened() => _emission.Source.Contains("given AuthorFollowed", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_the_read_model_followed() => _emission.Source.Contains("then readmodel AuthorWatch", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_no_command_at_all() => _emission.Application.Modules.Single().Features.SelectMany(_ => _.Slices).SelectMany(_ => _.Specifications).Single(_ => _.Name == "WhenAnAuthorIsFollowed").When.ShouldBeNull();
}
