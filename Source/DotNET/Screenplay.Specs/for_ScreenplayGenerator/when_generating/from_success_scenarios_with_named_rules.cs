// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_success_scenarios_with_named_rules : a_generated_document
{
    void Because()
    {
        const string Source = """
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using FluentValidation;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class NameValidator : CommandValidator<RegisterAuthor>
            {
                public NameValidator()
                {
                    RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage("Use a known name");
                }
                static bool IsKnownName(string name) => name == "Apollo";
            }
            """;
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_a_name_is_supplied
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                async Task Because() => await _scenario.Execute(new RegisterAuthor("Apollo"));
                [Fact] void should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "Apollo");
            }
            """;
        Generate(
            (Analyzed.SlicePath, Source),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", Scenario));
    }

    [Fact] void should_preserve_the_successful_scenario() => Result.Source.ShouldContain("specification WhenRegisteringAndANameIsSupplied");
    [Fact] void should_not_add_opaque_validation() => Result.Source.ShouldNotContain("rule IsKnownName");
    [Fact] void should_retain_declarative_validation() => Result.Source.ShouldContain("name not empty");
    [Fact] void should_report_the_command_and_reason() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Message.Contains("withheld to keep scenarios", StringComparison.Ordinal)).Message.ShouldContain("command 'RegisterAuthor'");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();

    [Fact]
    void should_withhold_the_rule_in_authoring_only_mode_too()
    {
        var authoring = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        authoring.Source.ShouldNotContain("rule IsKnownName");
        authoring.Source.ShouldContain("specification WhenRegisteringAndANameIsSupplied");
    }
}
