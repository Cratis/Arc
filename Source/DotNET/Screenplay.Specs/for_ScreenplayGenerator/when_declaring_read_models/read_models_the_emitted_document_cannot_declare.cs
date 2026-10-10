// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// What the emitter does with a placed read model it cannot declare faithfully - a model handed over by a host
/// rather than one analysis just produced. It leaves the read model out of every slice, says so, and withholds
/// authoring-only reads of that omitted declaration too.
/// </summary>
public class read_models_the_emitted_document_cannot_declare : Specification
{
    const string Namespace = "Library.Authors";

    [Fact]
    void should_leave_out_every_read_model_declared_under_a_shared_name()
    {
        var result = Emit(
            Slice("Registration", ReadModel("Order_Summary", "String")),
            Slice("Reporting", ReadModel("OrderSummary", "Int")));

        result.Source.ShouldNotContain("readmodel OrderSummary");
        result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableReadModel).ShouldEqual(2);
    }

    [Fact]
    void should_leave_out_a_read_model_named_like_a_type()
    {
        var result = Emit(ApplicationModel.Empty with
        {
            Domain = "Library",
            Types = [new TypeModel("Parcel", [new("Label", new("String", false, false))])],
            Slices = [Slice("Registration", ReadModel("Parcel", "String"))]
        });

        result.Source.ShouldNotContain("readmodel Parcel");
        result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableReadModel).Message.ShouldContain("'Parcel'");
    }

    [Fact]
    void should_withhold_an_authoring_read_of_one_it_leaves_out()
    {
        var read = new CommandReadModel("AuthorState", "state", "Id")
        {
            Namespace = $"{Namespace}.Registration",
            Properties = [new("Name", new("String", false, false))]
        };
        var command = new CommandModel("Register", null, [new("Id", new("String", false, false))], null, [], [], null, "Command.cs")
        {
            Authoring = new() { Reads = [read] }
        };
        var projection = new ProjectionModel("AuthorStateProjection", "AuthorState", "event-log", ProjectionAutoMapMode.Enabled, false, ProjectionScopeModel.Empty);
        var model = Application(Slice("Registration", ReadModel("AuthorState", "Mystery")) with { Commands = [command], Projections = [projection] });

        var result = new ScreenplayEmitter().Emit(model, new() { AuthoringOnlyConstructs = true });

        result.Source.ShouldNotContain("readmodel AuthorState");
        result.Source.ShouldNotContain("reads AuthorState");
        result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableReadModel).Message.ShouldContain("'Mystery'");
    }

    static ReadModelModel ReadModel(string name, string type) => new(name, [new("Value", new(type, false, false))]);

    static SliceModel Slice(string name, ReadModelModel readModel) =>
        SliceModel.Empty($"{Namespace}.{name}", name, SliceKind.StateView) with { ReadModels = [readModel] };

    static ApplicationModel Application(params SliceModel[] slices) => ApplicationModel.Empty with { Domain = "Library", Slices = slices };

    static ScreenplayEmission Emit(params SliceModel[] slices) => Emit(Application(slices));

    static ScreenplayEmission Emit(ApplicationModel model) => new ScreenplayEmitter().Emit(model, new());
}
