// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_system_identity : a_reacting_application
{
    const string Handler = "public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));";

    [Theory]
    [InlineData(null)]
    [InlineData("Librarian")]
    [InlineData("  exact role  ")]
    [InlineData("Audit\"\\trail\t\n\r\0é漢字")]
    public void should_preserve_presence_and_exact_roles(string? role)
    {
        var arguments = role is null ? string.Empty : Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(role, quote: true);
        GenerateWith($$"""[ExecuteCommandsAsSystem({{arguments}})] public class Welcomer : IReactor { {{Handler}} }""");

        Result.Source.ShouldContain("runs as system");
        var parsedRoles = new ScreenplayCompiler().Compile(Result.Source).Value!.Modules.SelectMany(module => module.Features)
            .SelectMany(Slices).SelectMany(slice => slice.Reactions).Single().RunsAs!.Roles;
        parsedRoles.ShouldContainOnly(role is null ? [] : [role]);
        Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().SystemRoles!.ShouldContainOnly(role is null ? [] : [role]);
        Result.Source.ShouldContain("invokes SendWelcome");
        AssertDocument();
    }

    [Fact]
    public void should_not_invent_identity_in_the_absence_of_the_attribute()
    {
        GenerateWith($$"""public class Welcomer : IReactor { {{Handler}} }""");

        Result.Source.ShouldNotContain("runs as");
        Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().SystemRoles.ShouldBeNull();
        AssertDocument();
    }

    [Theory]
    [InlineData("", "runs as system\n")]
    [InlineData("\"Derived\"", "runs as system role \"Derived\"")]
    public void should_replace_inherited_roles_with_the_nearest_declaration(string arguments, string expected)
    {
        GenerateWith($$"""
            [ExecuteCommandsAsSystem("Base")]
            public abstract class Parent;
            [ExecuteCommandsAsSystem({{arguments}})]
            public class Welcomer : Parent, IReactor { {{Handler}} }
            """);

        Result.Source.ShouldContain(expected);
        Result.Source.ShouldNotContain("role \"Base\"");
        AssertDocument();
    }

    [Fact]
    public void should_recover_inherited_roles_without_a_derived_declaration()
    {
        GenerateWith($$"""[ExecuteCommandsAsSystem("Base")] public abstract class Parent; public class Welcomer : Parent, IReactor { {{Handler}} }""");

        Result.Source.ShouldContain("runs as system role \"Base\"");
        AssertDocument();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\" \"")]
    [InlineData("\"Same\", \"Same\"")]
    [InlineData("new string[] { null }")]
    public void should_refuse_invalid_roles_without_executing_a_different_identity(string arguments)
    {
        GenerateWith($$"""[ExecuteCommandsAsSystem({{arguments}})] public class Welcomer : IReactor { {{Handler}} }""");

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("runs as");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionIdentity).Location.ShouldEqual("Library.Authors.Welcoming.Welcomer");
        AssertDocument();
    }

    static IEnumerable<SliceSyntax> Slices(FeatureSyntax feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
