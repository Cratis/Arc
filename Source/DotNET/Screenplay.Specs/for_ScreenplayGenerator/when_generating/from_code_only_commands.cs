// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_code_only_commands : a_generated_document
{
    [Theory]
    [InlineData("public void Handle(IService service) => service.Do(Name);")]
    [InlineData("public void Handle(IService service, AuthorRegistered existing) => service.Append(existing);")]
    public void should_omit_behavior_that_cannot_be_recovered(string handler)
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            public interface IService
            {
                void Do(string name);
                void Append(AuthorRegistered existing);
            }
            [Command] public record RegisterAuthor(string Name)
            {
            """ + handler + "}")));

        Result.Source.ShouldNotContain("command RegisterAuthor");
        Result.Source.ShouldNotContain("handler");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CommandBehaviorInCode);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        diagnostic.Message.ShouldContain("AuthoringOnlyConstructs");
        AssertDocument();

        var authoring = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        authoring.Source.ShouldContain("command RegisterAuthor");
        authoring.Source.ShouldContain("handler");
        authoring.Source.ShouldContain("file");
        new ScreenplayCompiler().Compile(authoring.Source).Success.ShouldBeTrue();

        var withSpecification = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Specifications = [new("CallingCode", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource("Austen"))]), [], [])]
            }).ToList()
        };
        var executable = new ScreenplayEmitter().Emit(withSpecification, new());
        executable.Source.ShouldNotContain("specification CallingCode");
        executable.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("command is not present");
        new ScreenplayCompiler().Compile(executable.Source).Success.ShouldBeTrue();
    }

    [Fact]
    public void should_keep_a_provably_empty_handler()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command] public record RegisterAuthor(string Name)
            {
                public void Handle() { }
            }
            """)));
        Result.Source.ShouldContain("command RegisterAuthor");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.CommandBehaviorInCode).ShouldBeEmpty();
        AssertDocument();
    }
}
