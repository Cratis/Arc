// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class ambiguous_authoring_declarations : Specification
{
    [Fact]
    void should_drop_operations_with_ambiguous_system_names()
    {
        var first = Operation("Notify", "global::First.IMailer");
        var second = Operation("Send", "global::Second.IMailer");
        var result = Emit(Command("Register", new() { Operations = [first] }), Command("Invite", new() { Operations = [second] }));
        result.Source.Contains("system Mailer", StringComparison.Ordinal).ShouldBeFalse();
        result.Source.Contains("produces operation", StringComparison.Ordinal).ShouldBeFalse();
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation && diagnostic.Message.Contains("Command 'Register'", StringComparison.Ordinal))
            .Message.ShouldContain("ambiguous ownership");
    }

    [Fact]
    void should_drop_mixed_keyed_and_unkeyed_routes_and_their_orphan_concepts()
    {
        var model = Application(
            Command("Register", new() { Route = new("Accounts", "Monthly", new("RouteId", false, false), new("MonthId", false, false), "Month") }),
            Command("Invite", new() { Route = new("Accounts", "Monthly", new("RouteId", false, false), null, null) })) with
        {
            Concepts = [Concept("RouteId"), Concept("MonthId"), Concept("Retained")]
        };
        var result = new ScreenplayEmitter().Emit(model, new() { AuthoringOnlyConstructs = true });
        result.Source.Contains("eventsource Accounts", StringComparison.Ordinal).ShouldBeFalse();
        result.Source.Contains("stream Accounts", StringComparison.Ordinal).ShouldBeFalse();
        result.Source.Contains("concept RouteId", StringComparison.Ordinal).ShouldBeFalse();
        result.Source.Contains("concept MonthId", StringComparison.Ordinal).ShouldBeFalse();
        result.Source.ShouldContain("concept Retained");
        result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute && diagnostic.Message.Contains("Command 'Register'", StringComparison.Ordinal))
            .Message.ShouldContain("incompatible identity or stream-id types");
    }

    [Fact]
    void should_drop_an_ambiguous_source_without_changing_unrelated_concepts()
    {
        var result = Emit(
            Command("Register", new() { Route = new("Accounts", "Monthly", new("Uuid", false, false), null, null) }),
            Command("Invite", new() { Route = new("Accounts", "Monthly", new("String", false, false), null, null) }));
        result.Source.Contains("eventsource Accounts", StringComparison.Ordinal).ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(2);
    }

    [Fact]
    void should_drop_unavailable_reads_and_operations_that_map_from_them()
    {
        var operation = Operation("Notify", "global::IMailer") with
        {
            Inputs = [new("Name", new("ReadOnlyName", false, false))],
            Mappings = [new("Name", new PropertyPathSource("state.Name"))]
        };
        var command = Command("Register", new()
        {
            Reads = [new("AuthorState", "state", "Id") { Namespace = "Library.Authors", Properties = [new("Name", new("ReadOnlyName", false, false))] }],
            Requirements = [new(new ComparisonCondition("state.Name", ComparisonKind.Equal, new LiteralSource("ready")), "Not ready")],
            Operations = [operation]
        }) with { Produces = [new("Registered", null, [new("Name", new PropertyPathSource("state.Name"))])] };
        var diagnostics = new ScreenplayDiagnostics();
        var conflicting = Command("Invite", new()
        {
            Reads = [new("AuthorState", "state", "Id") { Namespace = "Other.Authors", Properties = [new("OtherName", new("String", false, false))] }]
        });
        var resolved = AuthoringDeclarations.Resolve(Application(command, conflicting) with { Concepts = [Concept("ReadOnlyName")] }, diagnostics);
        var kept = resolved.Slices.Single().Commands.Single(command => command.Name == "Register");
        kept.Authoring!.Reads.ShouldBeEmpty();
        kept.Authoring.Requirements.ShouldBeEmpty();
        kept.Authoring.Operations.ShouldBeEmpty();
        kept.Produces.Single().Mappings.ShouldBeEmpty();
        resolved.Concepts.ShouldBeEmpty();
        diagnostics.All.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation).Message.ShouldContain("Command 'Register': operation 'Notify' depends on an unavailable read");
        diagnostics.All.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning && diagnostic.Message.Contains("Command 'Register'", StringComparison.Ordinal)).Location.ShouldEqual("Library.Authors");
        resolved.Slices.Single().Commands.Single(command => command.Name == "Invite").Authoring!.Reads.ShouldBeEmpty();
    }

    static OperationModel Operation(string name, string identity) => new(name, "Mailer", null, [], [], "Notify.cs", false) { SystemTypeIdentity = identity };
    static ConceptModel Concept(string name) => new(name, ScreenplayPrimitive.String, false, [], []);
    static CommandModel Command(string name, CommandAuthoringModel authoring) => new(name, null, [], null, [], [], null, "Command.cs") { Authoring = authoring };
    static ApplicationModel Application(params CommandModel[] commands) => ApplicationModel.Empty with
    {
        Domain = "Library",
        Slices = [SliceModel.Empty("Library.Authors", "Registration", SliceKind.StateChange) with { Commands = commands }]
    };
    static ScreenplayEmission Emit(params CommandModel[] commands) => new ScreenplayEmitter().Emit(Application(commands), new() { AuthoringOnlyConstructs = true });
}
