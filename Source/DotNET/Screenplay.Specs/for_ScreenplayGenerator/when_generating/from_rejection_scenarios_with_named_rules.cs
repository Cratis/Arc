// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_rejection_scenarios_with_named_rules : a_generated_document
{
    void Because()
    {
        const string Source = """
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using FluentValidation;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Email);
            [Command] public record RegisterAuthor(string Email)
            {
                public AuthorRegistered Handle() => new(Email);
            }
            public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
            {
                public RegisterAuthorValidator()
                {
                    RuleFor(c => c.Email).NotEmpty().WithMessage("Email is required").Must(IsCompanyEmail).WithMessage("Use a company email");
                }
                static bool IsCompanyEmail(string email) => email.EndsWith("@company.test");
            }
            """;
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_email_is_empty
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Result _result = null!;
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(""));
                [Fact] void should_not_succeed() => _result.ShouldHaveConstraintViolationFor("Email is required");
            }
            """;
        Generate(
            (Analyzed.SlicePath, Source),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            ("Library/Authors/Registration/when_registering/and_email_is_empty.cs", Scenario));
    }

    [Fact] void should_preserve_the_only_scenario() => Result.Source.ShouldContain("specification WhenRegisteringAndEmailIsEmpty");
    [Fact] void should_preserve_the_rejection() => Result.Source.ShouldContain("then error \"Email is required\"");
    [Fact] void should_not_add_opaque_validation() => Result.Source.ShouldNotContain("rule IsCompanyEmail");
    [Fact] void should_retain_declarative_validation() => Result.Source.ShouldContain("email not empty");
    [Fact] void should_report_the_withheld_command_rule() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Message.Contains("withheld to keep scenarios", StringComparison.Ordinal)).Message.ShouldContain("command 'RegisterAuthor'");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();

    [Fact]
    void should_withhold_the_rule_in_authoring_only_mode_too()
    {
        var authoring = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        authoring.Source.ShouldNotContain("rule IsCompanyEmail");
        authoring.Source.ShouldContain("specification WhenRegisteringAndEmailIsEmpty");
        authoring.Source.ShouldContain("then error");
    }
}
