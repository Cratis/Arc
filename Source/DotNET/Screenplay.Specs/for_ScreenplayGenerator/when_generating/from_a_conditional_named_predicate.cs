// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_conditional_named_predicate : a_generated_document
{
    [Theory]
    [InlineData("RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).When(c => c.CheckName).WithMessage(\"m\");")]
    [InlineData("RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).Unless(c => c.CheckName).WithMessage(\"m\");")]
    [InlineData("When(c => c.CheckName, () => { RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\"); });")]
    [InlineData("Unless(c => c.CheckName, () => { RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\"); });")]
    [InlineData("if (System.Environment.GetEnvironmentVariable(\"X\") != \"1\") return; RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\");")]
    [InlineData("if (System.Environment.GetEnvironmentVariable(\"X\") != \"1\") return; { RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\"); }")]
    [InlineData("if (System.Environment.GetEnvironmentVariable(\"X\") != \"1\") throw new NameCheckDisabled(); RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\");")]
    [InlineData("if (System.Environment.GetEnvironmentVariable(\"X\") != \"1\") goto Done; RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\"); Done: return;")]
    [InlineData("if (System.Environment.GetEnvironmentVariable(\"X\") != \"1\") return; RuleSet(\"default\", () => { RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage(\"m\"); });")]
    public void should_omit_the_predicate_rather_than_state_it_unconditionally(string declaration)
    {
        Generate((Analyzed.SlicePath, $$"""
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public class NameCheckDisabled : System.Exception { }
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor(string Name, bool CheckName)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
            {
                public RegisterAuthorValidator()
                {
                    {{declaration}}
                }
                static bool IsKnownName(string name) => name == "Apollo";
            }
            """));
        Result.Source.ShouldNotContain("rule IsKnownName");
        Result.Source.ShouldNotContain("name not empty");
        Result.Source.ShouldNotContain("message \"m\"");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("IsKnownName", StringComparison.Ordinal) && diagnostic.Message.Contains("conditionally", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}
