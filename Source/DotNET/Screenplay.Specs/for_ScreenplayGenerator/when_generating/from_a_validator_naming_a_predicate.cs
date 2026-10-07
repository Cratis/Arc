// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A method group with source can be attached as an opaque named rule; an inline lambda still cannot.
/// </summary>
public class from_a_validator_naming_a_predicate : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(command => command.Name).Must(is_known_name).WithMessage("Use a known name");
                RuleFor(command => command.Name).NotEmpty().Must(name => name.Length > 1).WithMessage("m");
            }

            static bool is_known_name(string name) => name == "Jane Austen";
        }
        """));

    [Fact] void should_name_the_predicate() => Result.Source.ShouldContain("name rule is_known_name message \"Use a known name\"");
    [Fact] void should_attach_the_source_file() => Result.Source.ShouldContain("file Feature/Slice/Slice.cs");
    [Fact] void should_not_claim_to_have_emitted_the_lambda() => Result.Source.ShouldNotContain("message \"m\"");
    [Fact] void should_keep_the_preceding_rule_without_the_lambdas_message() => Result.Source.ShouldContain("name not empty");
    [Fact] void should_report_the_lambda() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeTrue();
    [Fact] void should_not_report_the_named_predicate_as_unmappable() => Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("'Must'", StringComparison.Ordinal) && diagnostic.Message.Contains("is_known_name", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
