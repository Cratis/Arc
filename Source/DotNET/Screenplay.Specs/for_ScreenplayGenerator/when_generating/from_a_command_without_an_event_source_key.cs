// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_without_an_event_source_key : a_generated_document
{
    [Theory]
    [InlineData("AddChangeStreamItem", "Add")]
    [InlineData("UpdateChangeStreamItem", "Update")]
    public void should_not_treat_value_as_an_identifier_when_no_key_was_selected(string commandName, string methodName)
    {
        Generate((Analyzed.SlicePath, $$"""
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Commands.ModelBound;
            namespace Library.Authors.Registration;
            [Command, AllowAnonymous]
            public record {{commandName}}(int Id, string Label, int Value)
            {
                public void Handle() => ChangeStreamItem.{{methodName}}(Id, Label, Value);
            }
            public static class ChangeStreamItem
            {
                public static void Add(int id, string label, int value) { }
                public static void Update(int id, string label, int value) { }
            }
            """));

        var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
        command.Identifier.ShouldBeNull();
        command.Authoring!.Identifier.ShouldBeNull();
        Result.Source.ShouldContain($"command {commandName}");
        Result.Source.ShouldContain("value Int");
        Result.Source.ShouldContain("handler");
        Result.Source.ShouldNotContain("identifier");
        Result.Source.ShouldNotContain("produces");
        Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        new ScreenplayCompiler().Compile(Result.Source).Success.ShouldBeTrue();
        RoundTrip.IsStable.ShouldBeTrue();
        RoundTrip.Errors.ShouldBeEmpty();

        var authoring = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        authoring.Source.ShouldContain("value Int");
        authoring.Source.ShouldContain("handler");
        authoring.Source.ShouldNotContain("identifier");
        new ScreenplayCompiler().Compile(authoring.Source).Success.ShouldBeTrue();
    }
}
