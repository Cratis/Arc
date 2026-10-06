// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_event_producers_in_two_compilations : Specification
{
    const string OtherSource = """
        using Library.Authors.Registration;
        namespace Other;
        public static class Copies
        {
            public static AuthorRegistered Make(string name) => new(name);
        }
        """;

    ScreenplayGenerationResult _result;
    Compilation _commands;
    Compilation _other;

    void Establish()
    {
        _commands = Analyzed.Compile((Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)));
        _other = Analyzed.Project("Other", [_commands.ToMetadataReference()], ("Other/Other.cs", OtherSource));
    }

    void Because() => _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None), new ScreenplayEmitter())
        .Generate([_commands, _other], new ScreenplayOptions { Domain = "Library", Module = "Library" });

    [Fact] void should_analyze_valid_commands() => Analyzed.ErrorsIn(_commands).ShouldBeEmpty();
    [Fact] void should_analyze_valid_other_code() => Analyzed.ErrorsIn(_other).ShouldBeEmpty();
    [Fact] void should_count_the_site_in_the_other_compilation() => _result.Model.EventProducerCounts.Values.Single().ShouldEqual(2);
    [Fact] void should_keep_the_event_standalone() => _result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_compile_the_document() => new ScreenplayCompiler().Compile(_result.Source).Success.ShouldBeTrue();
}
