// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_ordinary_input_with_a_rule_factory : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_keep_the_ordinary_scenario_despite_an_unused_factory(bool authoring, bool capped)
    {
        const string Source = """
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            using FluentValidation;
            namespace Library.Authors.Registration;
            public record AuthorName(string Value) : ConceptAs<string>(Value), IGeneratable<AuthorName>
            {
                public static AuthorName New() => new(string.Concat("author-", Guid.NewGuid().ToString()));
                public static implicit operator AuthorName(string value) => new(value);
            }
            public class AuthorNameValidator : Cratis.Arc.Validation.ConceptValidator<AuthorName>
            {
                public AuthorNameValidator() => RuleFor(value => value.Value).NotEmpty().Must(IsKnownName);
                static bool IsKnownName(string name) => name == "Apollo";
            }
            [Command] public record EchoName(AuthorName Name)
            {
                public AuthorName Handle() => Name;
            }
            """;
        const string Testing = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            namespace Cratis.Arc.Testing.Commands;
            public class CommandScenario<TCommand>
            {
                public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
            }
            """;
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Xunit;
            namespace Library.Authors.Registration.when_echoing;
            public class and_a_name_is_supplied
            {
                readonly CommandScenario<EchoName> _scenario = new();
                CommandResult _result = null!;
                async Task Because() => _result = await _scenario.Execute(new EchoName("Apollo"));
                [Fact] void should_echo_the_name() => Assert.Equal(new AuthorName("Apollo"), ((CommandResult<AuthorName>)_result).Response);
            }
            """;
        Generate(
            new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = capped ? SemanticVersion.V20 : null },
            (Analyzed.SlicePath, Source),
            ("Library/Testing/Scenario.cs", Testing),
            ("Library/Authors/Registration/when_echoing/and_a_name_is_supplied.cs", Scenario));
        Assert.True(Result.Source.Contains("specification WhenEchoingAndANameIsSupplied", StringComparison.Ordinal), $"{Result.Source}\n{string.Join('\n', Result.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        Result.Source.ShouldContain("not empty");
        Result.Source.ShouldNotContain("rule IsKnownName");
        Result.Source.ShouldNotContain("generate rule");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("withheld to keep scenarios", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
        if (!authoring)
        {
            AssertDocument();
            Run("WhenEchoingAndANameIsSupplied").Passed.ShouldBeTrue();
        }
    }
}
