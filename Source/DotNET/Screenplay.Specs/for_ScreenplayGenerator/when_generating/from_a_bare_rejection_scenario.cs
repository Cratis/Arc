// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_bare_rejection_scenario : a_generated_document
{
    const string Name = "WhenRegisteringAndTheNameIsEmpty";

    [Theory]
    [InlineData("_result.IsValid.ShouldBeFalse()")]
    [InlineData("_result.ShouldHaveValidationErrors()")]
    [InlineData("_result.ShouldHaveValidationErrorFor(Reason())")]
    [InlineData("{ _result.IsValid.ShouldBeFalse(); _result.IsSuccess.ShouldBeFalse(); }")]
    public void should_bind_and_run_a_rejection_without_a_reason(string assertion)
    {
        GenerateScenario(assertion);
        Assert.True(Result.Source.Split('\n').Any(line => line.Trim() == "then error"), Result.Source);
        Result.Source.ShouldNotContain("then error \"\"");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
        AssertDocument();
        var run = Run(Name);
        Assert.True(run.Passed, string.Join(Environment.NewLine, run.Failures) + Environment.NewLine + Result.Source);
    }

    [Theory]
    [InlineData("_result.ShouldNotBeAuthorized()")]
    [InlineData("_result.ShouldHaveExceptions()")]
    [InlineData("_result.ShouldNotBeSuccessful()")]
    [InlineData("_result.IsSuccess.ShouldBeFalse()")]
    [InlineData("{ _result.IsValid.ShouldBeFalse(); _result.ShouldNotBeAuthorized(); }")]
    [InlineData("{ _result.IsValid.ShouldBeFalse(); _result.ShouldHaveExceptions(); }")]
    public void should_omit_a_failure_that_does_not_assert_only_a_validation_rejection(string assertion)
    {
        GenerateScenario(assertion);
        Result.Source.ShouldNotContain("then error");
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {Name}");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }

    [Fact]
    public void should_not_treat_an_unrelated_validity_property_as_a_command_rejection()
    {
        GenerateScenario("_unrelated.IsValid.ShouldBeFalse()");
        Result.Source.ShouldNotContain("then error");
        Result.Source.ShouldNotContain($"specification {Name}");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }

    void GenerateScenario(string assertion)
    {
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal) + """

            namespace Cratis.Arc.Testing.Commands
            {
                public static class CommandResultShouldExtensions
                {
                    public static void ShouldHaveValidationErrors(this Cratis.Arc.Commands.CommandResult result) { }
                    public static void ShouldHaveValidationErrorFor(this Cratis.Arc.Commands.CommandResult result, string message) { }
                    public static void ShouldNotBeSuccessful(this Cratis.Arc.Commands.CommandResult result) { }
                    public static void ShouldNotBeAuthorized(this Cratis.Arc.Commands.CommandResult result) { }
                    public static void ShouldHaveExceptions(this Cratis.Arc.Commands.CommandResult result) { }
                }
            }
            """;
        const string Source = """
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using FluentValidation;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor([Key] string Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
            {
                public RegisterAuthorValidator()
                {
                    RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required");
                }
            }
            """;
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_the_name_is_empty
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Cratis.Arc.Commands.CommandResult _result = null!;
                readonly Status _unrelated = new(false);
                public record Status(bool IsValid);
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("current", ""));
                static string Reason() => "Name is required";
                [Fact] void should_reject_the_command() {{(assertion.StartsWith('{') ? assertion : "=> " + assertion + ";")}}
            }
            """;
        Generate(
            (Analyzed.SlicePath, Source),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_the_name_is_empty.cs", scenario));
    }
}
