// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_validation_metadata_before_a_message : a_generated_document
{
    [Theory]
    [InlineData("WithErrorCode(\"X\")")]
    [InlineData("WithSeverity(Severity.Warning)")]
    [InlineData("WithName(\"Author name\")")]
    [InlineData("OverridePropertyName(\"AuthorName\")")]
    [InlineData("WithState(command => command.Name)")]
    [InlineData("Cascade(CascadeMode.Stop)")]
    public void should_keep_the_message_on_the_preceding_validator(string modifier)
    {
        var chain = modifier.StartsWith("Cascade", StringComparison.Ordinal)
            ? $"RuleFor(command => command.Name).{modifier}.NotEmpty()"
            : $"RuleFor(command => command.Name).NotEmpty().{modifier}";
        Generate((Analyzed.SlicePath, $$"""
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
            public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
            {
                public RegisterAuthorValidator()
                {
                    {{chain}}.WithMessage("Use a name");
                }
            }
            """));
        Result.Source.ShouldContain("name not empty message \"Use a name\"");
        AssertDocument();
    }
}
