// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_response_only_scenario_for_an_event_producing_command : a_generated_document
{
    const string TupleHandler = "public (AuthorRegistered, string) Handle() => (new(Name), Name);";

    void Because() => GenerateScenario(TupleHandler, withRule: true);

    [Fact] void should_keep_the_command_response() => Result.Source.ShouldContain("returns name");
    [Fact] void should_not_state_that_the_command_records_no_facts() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_explain_why_the_scenario_was_left_out() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("not proven to record no facts");
    [Fact] void should_not_withhold_the_named_rule_for_a_scenario_that_is_left_out() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule).ShouldBeFalse();
    [Fact] void should_retain_the_named_rule() => Result.Source.ShouldContain("rule IsKnownName");
    [Fact] void should_not_prove_the_handler_free_of_facts() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().HasNoFactBehavior.ShouldBeFalse();
    [Fact] void should_bind_and_round_trip() => AssertDocument();

    [Theory]
    [InlineData(TupleHandler, false)]
    [InlineData("public (System.Collections.Generic.IEnumerable<AuthorRegistered>, string) Handle() => (new[] { new AuthorRegistered(Name) }, Name);", false)]
    [InlineData("public (AuthorRegistered[], string) Handle() => ([new AuthorRegistered(Name)], Name);", false)]
    [InlineData("public string Handle() => Echo(new AuthorRegistered(Name));", false)]
    [InlineData("public object Handle() => Name;", false)]
    [InlineData("public string Handle() => Name;", true)]
    public void should_prove_no_facts_only_for_handlers_that_cannot_carry_an_event(string handler, bool proven)
    {
        GenerateScenario(handler, withRule: false);
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().HasNoFactBehavior.ShouldEqual(proven);
        Result.Source.Contains("specification WhenRegisteringAndANameIsSupplied", StringComparison.Ordinal).ShouldEqual(proven);
    }

    void GenerateScenario(string handler, bool withRule)
    {
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal);
        var rule = withRule
            ? """
              public class NameValidator : CommandValidator<RegisterAuthor>
              {
                  public NameValidator()
                  {
                      RuleFor(c => c.Name).NotEmpty().Must(IsKnownName).WithMessage("Use a known name");
                  }
                  static bool IsKnownName(string name) => name == "Apollo";
              }
              """
            : string.Empty;
        var slice = $$"""
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using FluentValidation;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor([Cratis.Chronicle.Keys.Key] string Id, string Name)
            {
                {{handler}}
                static string Echo(AuthorRegistered registered) => registered.Name;
            }
            {{rule}}
            """;
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Specifications;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_a_name_is_supplied
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Cratis.Arc.Commands.CommandResult _result = null!;
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("author", "Apollo"));
                [Fact] void should_return_the_name() => ((CommandResult<object>)_result).Response.ShouldEqual("Apollo");
            }
            """;
        var scenario = handler.StartsWith("public object", StringComparison.Ordinal) ? Scenario : Scenario.Replace("CommandResult<object>", "CommandResult<string>", StringComparison.Ordinal);
        (string Path, string Text)[] sources =
        [
            (Analyzed.SlicePath, slice),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", scenario)
        ];
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
    }
}
