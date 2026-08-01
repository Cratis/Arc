// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The one failure mode worse than a document that says too little is a document that does not compile. A tag and a
/// validation message are both free text the developer wrote, and both are printed unescaped onto a line of their
/// own, so a line break in either of them splits the construct in two and the rest of the file is read as garbage.
/// </summary>
public class from_source_carrying_text_the_grammar_cannot_hold : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [EventType]
        [Tag("carries\na line break")]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage("An author must have a name.\nGive it one.");
            }
        }
        """;

    static readonly (string Path, string Text)[] _sources = [("Library/Authors/Registration/Registration.cs", Source)];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_tag_on_one_line() => _result.Source.ShouldContain("tag \"carries a line break\"");
    [Fact] void should_keep_the_message_on_one_line() => _result.Source.ShouldContain("message \"An author must have a name. Give it one.\"");
}
