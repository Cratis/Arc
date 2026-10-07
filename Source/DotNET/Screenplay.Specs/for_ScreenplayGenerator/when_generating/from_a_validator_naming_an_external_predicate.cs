// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A predicate outside the inferred source root cannot be attached as a portable rule file.
/// </summary>
public class from_a_validator_naming_an_external_predicate : a_generated_document
{
    const string Source = """
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
                    RuleFor(command => command.Name).Must(Predicates.IsKnown);
                }
            }
            """;

    const string Predicate = """
            namespace Library.Authors.Registration;

            public static class Predicates
            {
                public static bool IsKnown(string name) => name == "Jane Austen";
            }
            """;

    void Because() => Generate(
        ("/application/Feature/Slice/Slice.cs", Source),
        ("/external/Predicates.cs", Predicate));

    [Fact] void should_not_emit_a_named_rule() => Result.Source.ShouldNotContain("rule IsKnown");
    [Fact] void should_not_emit_the_absolute_path() => Result.Source.ShouldNotContain("/external/Predicates.cs");
    [Fact] void should_report_the_unmappable_rule() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeTrue();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
