// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// FluentValidation lets a message be a value or a lambda producing one, and keeping messages in one place behind a
/// constant - <c>WithMessage(_ =&gt; Messages.NameRequired)</c> - is the common way an application writes them. The
/// lambda body is a plain reference the semantic model reads a compile-time constant from, so the message is
/// recovered exactly as it would be from a value written inline.
/// </summary>
public class a_validator_whose_message_is_a_lambda_returning_a_constant : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        public static class AuthorMessages
        {
            public const string NameRequired = "An author must have a name";
        }

        [Command]
        public record RegisterAuthor(string Name, string Email)
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
                RuleFor(_ => _.Email).EmailAddress().WithMessage(_ => "An email must look like one");
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

    ValidationRuleModel Rule(ValidationRuleKind kind) => _rules.First(_ => _.Kind == kind);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_a_message_from_a_lambda_returning_a_constant() => Rule(ValidationRuleKind.NotEmpty).Message.ShouldEqual("An author must have a name");
    [Fact] void should_recover_a_message_from_a_lambda_returning_a_literal() => Rule(ValidationRuleKind.Matches).Message.ShouldEqual("An email must look like one");
    [Fact] void should_leave_no_message_unrecovered() => _analysis.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.UnmappableValidationRule);
}
