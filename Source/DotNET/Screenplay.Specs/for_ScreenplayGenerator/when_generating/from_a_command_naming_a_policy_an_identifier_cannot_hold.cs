// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An application names its policies and roles whatever it likes - <c>can-reserve</c>, <c>reader</c> - while the
/// grammar accepts a policy reference only as a Pascal cased identifier. Both the reference and the declaration are
/// converted the same way, so a name the grammar cannot hold yields a document that still compiles and still declares
/// everything it refers to.
/// </summary>
public class from_a_command_naming_a_policy_an_identifier_cannot_hold : Specification
{
    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Lending/Reserving/Reserving.cs", """
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;

            namespace Library.Lending.Reserving;

            [EventType]
            public record BookReserved(string Isbn);

            [Command]
            [Authorize(Policy = "can-reserve", Roles = "reader")]
            public record ReserveBook(string Isbn)
            {
                public BookReserved Handle() => new(Isbn);
            }
            """)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool HasLine(string text) => _result.Source.Split('\n').Any(_ => string.Equals(_.Trim(), text, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_refer_to_the_policy_by_a_name_the_grammar_accepts() => HasLine("CanReserve").ShouldBeTrue();
    [Fact] void should_declare_the_policy_it_refers_to() => HasLine("policy CanReserve").ShouldBeTrue();
    [Fact] void should_refer_to_the_role_by_a_name_the_grammar_accepts() => HasLine("authorize Reader").ShouldBeTrue();
    [Fact] void should_keep_the_role_the_application_really_names() => HasLine("require role \"reader\"").ShouldBeTrue();
    [Fact] void should_report_that_nothing_registers_the_policy() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable]);
}
