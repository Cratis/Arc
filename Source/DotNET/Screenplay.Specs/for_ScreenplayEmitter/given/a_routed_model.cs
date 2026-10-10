// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

public class a_routed_model : an_emitter
{
    protected static readonly TypeReferenceModel Text = new("String", false, false);
    protected CommandModel _command;
    protected ScreenplayEmission _result;

    void Establish() => _command = new("Register", null, [new("Id", Text), new("Month", Text), new("Name", Text)], null, [], [new("Registered", null, [new("Name", new PropertyPathSource("Name"))]) { UsesCommandContext = true }], null, "Register.cs")
    {
        Identifier = "Id",
        Authoring = new() { Route = new("Account", "Transactions", Text, Text, "Month") }
    };

    protected ApplicationModel Model(IEnumerable<SpecificationModel>? specifications = null) => new(
        "Library",
        "Library",
        [],
        [],
        [SliceModel.Empty("Library.Authors.Registration", "Registration", SliceKind.StateChange) with
        {
            Commands = [_command],
            Events = [new("Registered", [new("Name", Text)], [])],
            Specifications = specifications ?? []
        }],
        []);

    protected void AssertBinds()
    {
        var verified = new ScreenplayVerifier().Verify(_result.Source);
        verified.Errors.ShouldBeEmpty();
        verified.BindingDiagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        RoundTrip.For(_result.Application).IsStable.ShouldBeTrue();
    }
}
