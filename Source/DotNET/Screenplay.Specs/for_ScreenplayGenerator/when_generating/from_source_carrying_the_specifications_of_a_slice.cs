// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario recovered from source has to survive being written down: the name it is declared under, the events it
/// starts from, the command it issues and the rejection it expects are each a line the language reads back, and a
/// scenario is the one construct where the value of a property and the name of a step sit one indentation apart.
/// This asks the whole way through, from source to printed text and back again through the compiler.
/// </summary>
public class from_source_carrying_the_specifications_of_a_slice : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Invoicing.Issuing;

        [EventType]
        public record InvoiceIssued(string Number, int Lines);

        [ReadModel]
        public record Invoice(string Id, string Number);

        [Command]
        public record IssueInvoice(string Number, int Lines)
        {
            public InvoiceIssued Handle() => new(Number, Lines);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Invoicing.Issuing;
        using Xunit;

        namespace Library.Invoicing.Issuing.when_issuing;

        public class and_the_invoice_has_no_lines
        {
            public const string OneInvoicePerNumber = "one-invoice-per-number";

            readonly CommandScenario<IssueInvoice> _scenario = new();
            Result _result = null!;

            void Establish()
            {
                _scenario.Given.ForEventSource("invoice").Events(new InvoiceIssued("2026-1", 3));
                _scenario.Given.ForEventSource("invoice").ReadModel(new Invoice("invoice", "2026-1"));
            }

            async Task Because() => _result = await _scenario.Execute(new IssueInvoice("2026-2", 0));

            [Fact] void should_not_issue_the_invoice() => _result.ShouldHaveConstraintViolationFor(OneInvoicePerNumber);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Invoicing/Issuing/Issuing.cs", Slice),
        ("Library/Invoicing/Issuing/when_issuing/and_the_invoice_has_no_lines.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
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

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_declare_the_scenario_under_the_words_the_source_wrote() => Says("specification WhenIssuingAndTheInvoiceHasNoLines").ShouldBeTrue();
    [Fact] void should_state_the_event_it_starts_from() => Says("given InvoiceIssued").ShouldBeTrue();
    [Fact] void should_state_the_read_model_it_starts_from() => Says("given readmodel Invoice").ShouldBeTrue();
    [Fact] void should_state_the_values_it_starts_from() => Says("number = \"2026-1\"").ShouldBeTrue();
    [Fact] void should_state_the_command_it_issues() => Says("when IssueInvoice").ShouldBeTrue();
    [Fact] void should_state_the_values_the_command_carries() => Says("lines = 0").ShouldBeTrue();
    [Fact] void should_state_the_rejection_and_the_reason_named_for_it() => Says("then error \"one-invoice-per-number\"").ShouldBeTrue();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
