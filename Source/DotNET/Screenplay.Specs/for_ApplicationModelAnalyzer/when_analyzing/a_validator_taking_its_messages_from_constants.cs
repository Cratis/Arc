// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Wherever messages are declared once and named from every validator rather than repeated, the message reaches the
/// rule through a lambda - and the lambda computes nothing, it names a constant the compiler already substituted.
/// Reading only the direct form left a document with no message on any rule at all in exactly the applications that
/// took the most care over them. The lambda that really does build its text while the request runs is the one shape
/// there is nothing to write down for, and it stays reported.
/// </summary>
public class a_validator_taking_its_messages_from_constants : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        public static class AuthorMessages
        {
            public const string NameRequired = "An author must have a name";
            public static readonly string EmailRequired = "An author must have an email address";
        }

        [Command]
        public record RegisterAuthor(string Name, string Pen, string Email, string Nickname)
        {
            public void Handle()
            {
            }
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage(_ => AuthorMessages.NameRequired);
                RuleFor(_ => _.Pen).NotEmpty().WithMessage(_ => "An author must have a pen name");
                RuleFor(_ => _.Email).NotEmpty().WithMessage(_ => AuthorMessages.EmailRequired);
                RuleFor(_ => _.Nickname).NotEmpty().WithMessage(command => $"'{command.Nickname}' will not do");
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ValidationRuleModel> _rules;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _rules = _analysis.Slice().Commands.First().Validations;
    }

    ValidationRuleModel Rule(string property) => _rules.First(_ => _.Property == property);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_state_every_rule_a_message_was_written_after() => _rules.Select(_ => _.Property).ShouldContainOnly(["Name", "Pen", "Email", "Nickname"]);
    [Fact] void should_recover_a_message_a_constant_holds() => Rule("Name").Message.ShouldEqual("An author must have a name");
    [Fact] void should_recover_a_message_written_into_the_lambda() => Rule("Pen").Message.ShouldEqual("An author must have a pen name");
    [Fact] void should_leave_a_message_only_the_running_application_holds_off_its_rule() => Rule("Email").Message.ShouldBeNull();
    [Fact] void should_leave_a_message_built_from_the_value_off_its_rule() => Rule("Nickname").Message.ShouldBeNull();
    [Fact] void should_report_only_the_messages_it_could_not_write_down() => _analysis.Diagnostics.Select(_ => _.Code).Distinct(StringComparer.Ordinal).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
    [Fact] void should_report_one_for_each_of_them() => _analysis.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_say_which_message_it_was() => _analysis.Diagnostics.Any(_ => _.Message.Contains("AuthorMessages.EmailRequired", StringComparison.Ordinal)).ShouldBeTrue();
}
