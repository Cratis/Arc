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

    void GenerateScenario(string assertion)
    {
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
                Result _result = null!;
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("current", ""));
                [Fact] void should_reject_the_command() => {{assertion}};
            }
            """;
        Generate(
            (Analyzed.SlicePath, Source),
            (IntegrationTesting.Path, IntegrationTesting.Source.Replace("public bool IsSuccess => true;", "public bool IsSuccess => true; public bool IsValid => true;", StringComparison.Ordinal)),
            ("Library/Authors/Registration/when_registering/and_the_name_is_empty.cs", scenario));
    }
}
