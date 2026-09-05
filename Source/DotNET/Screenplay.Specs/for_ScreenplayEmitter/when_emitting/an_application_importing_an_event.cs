// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// The Screenplay compiler reads the last segment of an import as the name of an event that is known, which is
/// exactly what a reactor observing an event of another bounded context needs. Compiling the document back is what
/// proves it - without the import the language reports the trigger as an event nothing declares.
/// </summary>
public class an_application_importing_an_event : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with
        {
            Imports = ["Partners.Contracts.InvitationToJoinAdaAccepted"],
            Slices =
            [
                .. LibraryApplication.Slices(),
                SliceModel.Empty("Library.Admin.Invitations", "Invitations", SliceKind.Automation) with
                {
                    Reactors =
                    [
                        new ReactorModel(
                            "AcceptedInvitationProvisioner",
                            ["InvitationToJoinAdaAccepted"],
                            false,
                            "Admin/Invitations/Provision.cs")
                    ]
                }
            ]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_declare_the_import() => _emission.Source.Contains("import Partners.Contracts.InvitationToJoinAdaAccepted", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_observe_it_from_the_reactor() => _emission.Source.Contains("when InvitationToJoinAdaAccepted", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_leave_the_language_nothing_to_warn_about() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_report_nothing_as_unmappable() => _emission.Diagnostics.ShouldBeEmpty();
}
