// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_bare_rejection_scenario : a_generated_document
{
    const string Name = "WhenRegisteringAndTheNameIsEmpty";

    [Theory]
    [InlineData("_result.IsSuccess.ShouldBeFalse()")]
    [InlineData("_result.IsValid.ShouldBeFalse()")]
    [InlineData("_result.ShouldNotBeSuccessful()")]
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
        var validatesResult = assertion == "_result.IsValid.ShouldBeFalse()";
        var testing = validatesResult
            ? IntegrationTesting.Source.Replace(
                "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
                "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
                StringComparison.Ordinal)
            : IntegrationTesting.Source;
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
                {{(validatesResult ? "Cratis.Arc.Commands.CommandResult" : "Result")}} _result = null!;
                readonly Status _unrelated = new(false);
                public record Status(bool IsValid);
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("current", ""));
                [Fact] void should_reject_the_command() => {{assertion}};
            }
            """;
        Generate(
            (Analyzed.SlicePath, Source),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_the_name_is_empty.cs", scenario));
    }
}
