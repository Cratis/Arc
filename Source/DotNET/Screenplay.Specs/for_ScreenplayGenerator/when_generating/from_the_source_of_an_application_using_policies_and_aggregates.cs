// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Everything a named policy, a length range and an aggregate root recover has to survive all the way into text the
/// Screenplay compiler accepts without a diagnostic - and printing that text again has to yield the same bytes. A
/// policy the document refers to but never declares would compile with a warning, and a warning in generated output
/// is a generated output nobody trusts.
/// </summary>
public class from_the_source_of_an_application_using_policies_and_aggregates : Specification
{
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(PolicyAndAggregateSource.All()), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    bool HasLine(string text) => _result.Source.Split('\n').Any(_ => string.Equals(_.Trim(), text, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(PolicyAndAggregateSource.All()).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_refer_to_the_policy_the_command_names() => Says("authorize CanReserve").ShouldBeTrue();
    [Fact] void should_declare_the_policy_it_refers_to() => Says("policy CanReserve").ShouldBeTrue();
    [Fact] void should_declare_what_the_policy_requires() => HasLine("require role \"Librarian\" and claim \"branch\" matches central").ShouldBeTrue();
    [Fact] void should_declare_the_several_values_of_one_requirement_as_alternatives() => HasLine("require role \"Librarian\" or role \"Archivist\"").ShouldBeTrue();
    [Fact] void should_ask_for_a_role_before_the_policy_it_is_asked_with() => HasLine("authorize Librarian").ShouldBeTrue();
    [Fact] void should_ask_for_the_policy_alongside_the_role() => HasLine("SeniorStaff").ShouldBeTrue();
    [Fact] void should_state_what_a_command_governed_by_an_aggregate_root_produces() => Says("produces BookReserved").ShouldBeTrue();
    [Fact] void should_map_the_produced_event_from_the_command_input() => Says("member = memberId").ShouldBeTrue();
    [Fact] void should_carry_the_message_of_a_length_range_on_its_lower_bound() => Says("isbn min 10 message \"An ISBN is between 10 and 13 characters\"").ShouldBeTrue();
    [Fact] void should_carry_the_message_of_a_length_range_on_its_upper_bound() => Says("isbn max 13 message \"An ISBN is between 10 and 13 characters\"").ShouldBeTrue();
    [Fact] void should_report_nothing_as_unmappable() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
