// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_rule_set_predicates : a_generated_document
{
    [Theory]
    [InlineData("\"draft\"", false)]
    [InlineData("\"default,draft\"", false)]
    [InlineData("GetRuleSetName()", false)]
    [InlineData("\"default\"", true)]
    [InlineData("\"DEFAULT\"", true)]
    [InlineData("DefaultRuleSet", true)]
    public void should_emit_named_predicates_only_in_a_proven_default_rule_set(string name, bool retained)
    {
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
                    const string DefaultRuleSet = "default";
                    RuleSet({{name}}, () =>
                    {
                        RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage("Use a known name");
                    });
                }
                static string GetRuleSetName() => "default";
                static bool IsKnownName(string name) => name == "Apollo";
            }
            """));
        Result.Source.Contains("name not empty", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.Contains("rule IsKnownName", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Source.Contains("message \"Use a known name\"", StringComparison.Ordinal).ShouldEqual(retained);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule &&
            diagnostic.Message.Contains("IsKnownName", StringComparison.Ordinal) && diagnostic.Message.Contains("conditionally", StringComparison.Ordinal)).ShouldEqual(!retained);
        AssertDocument();
    }
}
