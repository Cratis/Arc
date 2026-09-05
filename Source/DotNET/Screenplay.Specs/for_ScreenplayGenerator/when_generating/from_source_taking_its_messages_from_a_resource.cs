// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A key is written onto the message line unquoted, which is the one thing about it the language reads differently
/// from text. A quoted message is text whatever it holds, so a message that cannot be written is a message that reads
/// wrong; a reference that cannot be written is a line the compiler rejects, and a rejected line takes the whole
/// document with it. So the document a resource message ends up in is read back rather than only inspected.
/// </summary>
public class from_source_taking_its_messages_from_a_resource : Specification
{
    const string Source = """
        using System.Globalization;
        using System.Resources;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using FluentValidation;

        namespace Library.Authors.Registration;

        internal class AuthorMessages
        {
            static ResourceManager resourceMan;
            static CultureInfo resourceCulture;

            internal static ResourceManager ResourceManager
            {
                get
                {
                    if (object.ReferenceEquals(resourceMan, null))
                    {
                        resourceMan = new ResourceManager("Library.AuthorMessages", typeof(AuthorMessages).Assembly);
                    }
                    return resourceMan;
                }
            }

            internal static string Registration_NameRequired
            {
                get
                {
                    return ResourceManager.GetString("Registration_NameRequired", resourceCulture);
                }
            }
        }

        [EventType]
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
                RuleFor(_ => _.Name).NotEmpty().WithMessage(_ => AuthorMessages.Registration_NameRequired);
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
    [Fact] void should_write_the_key_unquoted() => _result.Source.ShouldContain("message $strings.AuthorMessages.Registration_NameRequired");
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_report_nothing_it_left_out() => _result.Diagnostics.ShouldBeEmpty();
}
