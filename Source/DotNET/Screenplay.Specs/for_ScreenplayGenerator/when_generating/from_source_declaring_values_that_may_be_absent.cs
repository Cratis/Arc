// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Whether a value may be absent is written in two different ways in C# - a reference type annotated as nullable and
/// a value type wrapped in <c>Nullable</c> - and in one way in Screenplay, a trailing question mark. Stripping the
/// wrapper without carrying what it said across leaves a document claiming every value is always there, which is a
/// shape the application does not have; carrying it across but naming the wrapper leaves a type nothing declares.
/// This asks the whole way through, from source to printed text, in every position a type reference is written in.
/// </summary>
public class from_source_declaring_values_that_may_be_absent : Specification
{
    const string Invoicing = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Invoicing.Grouping;

        public record InvoiceGroupKey(string Value) : ConceptAs<string>(Value);

        public record InvoiceNumber(int Value) : ConceptAs<int>(Value);

        public enum InvoiceStanding
        {
            Draft,
            Issued
        }

        [EventType]
        public record InvoiceGrouped(InvoiceGroupKey? Grouping, InvoiceStanding? Standing, IEnumerable<InvoiceNumber?> Numbers);

        [Command]
        public record GroupInvoice(InvoiceGroupKey? Reference, InvoiceStanding? Standing, IEnumerable<InvoiceNumber?> Numbers)
        {
            public InvoiceGrouped Handle() => new(Reference, Standing, Numbers);
        }

        [ReadModel]
        public record InvoiceGroup
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<InvoiceGroup> GroupsByKey(InvoiceGroupKey? groupKey) => [];
        }
        """;

    static readonly (string Path, string Text)[] _sources = [("Library/Invoicing/Grouping/Grouping.cs", Invoicing)];

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
    [Fact] void should_mark_an_optional_concept_on_a_command() => Says("reference InvoiceGroupKey?").ShouldBeTrue();
    [Fact] void should_mark_an_optional_concept_on_an_event() => Says("grouping InvoiceGroupKey?").ShouldBeTrue();
    [Fact] void should_mark_an_optional_enumeration() => Says("standing InvoiceStanding?").ShouldBeTrue();
    [Fact] void should_mark_a_collection_of_optional_values() => Says("numbers InvoiceNumber[]?").ShouldBeTrue();
    [Fact] void should_mark_an_optional_parameter_of_a_query() => Says("by groupKey InvoiceGroupKey?").ShouldBeTrue();
    [Fact] void should_never_name_the_wrapper_a_value_may_be_absent_behind() => Says("Nullable").ShouldBeFalse();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
