// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_success_scenario_with_command_values : a_generated_document
{
    [Theory]
    [InlineData("public (AuthorId, AuthorRegistered) Handle() { var authorId = new AuthorId(Guid.NewGuid()); return (authorId, new(Name)); }", false)]
    [InlineData("public (AuthorId, AuthorRegistered) Handle() { var authorId = new AuthorId(Guid.NewGuid()); return (authorId, new(Name)); }", true)]
    [InlineData("public (AuthorRegistered, string) Handle() => (new(Name), Name);", true)]
    [InlineData("public (AuthorRegistered, string) Handle() => (new(Name), Name.ToUpperInvariant());", true)]
    public void should_keep_legacy_productions_and_success_scenarios(string handler, bool responseAssertion)
    {
        GenerateScenario(handler, responseAssertion);
        Result.Source.ShouldContain("specification WhenRegisteringAndANameIsSupplied");
        Result.Source.ShouldContain("then AuthorRegistered");
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("for authorId");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        if (!handler.Contains("ToUpperInvariant", StringComparison.Ordinal))
        {
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("withheld to keep successful scenarios", StringComparison.Ordinal)).ShouldBeTrue();
        }
        AssertDocument();
    }

    [Fact]
    public void should_omit_a_response_assertion_scenario_only_when_the_emitted_command_has_returns()
    {
        GenerateScenario("public (AuthorRegistered, string) Handle() => (new(Name), Name);", true);
        var emitted = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        emitted.Source.ShouldContain("returns name");
        emitted.Source.ShouldNotContain("specification WhenRegisteringAndANameIsSupplied");
        emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("CommandResult.Response");
    }

    void GenerateScenario(string handler, bool responseAssertion)
    {
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal);
        var scenario = $$"""
                using System.Threading.Tasks;
                using Cratis.Arc.Commands;
                using Cratis.Arc.Testing.Commands;
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Chronicle.Testing.EventSequences;
                using Library.Authors.Registration;
                using Xunit;
                namespace Library.Authors.Registration.when_registering;
                public class and_a_name_is_supplied
                {
                    readonly CommandScenario<RegisterAuthor> _scenario = new();
                    Cratis.Arc.Commands.CommandResult _result = null!;
                    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Apollo"));
                    [Fact] Task should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "Apollo");
                    {{(responseAssertion ? "[Fact] void should_return_the_name() => Assert.Equal(\"Apollo\", ((CommandResult<string>)_result).Response);" : string.Empty)}}
                }
                """;
        Generate(
            (Analyzed.SlicePath, IdentifierSources.With("[Command] public record RegisterAuthor(string Name) { " + handler + " }")),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", scenario));
    }
}
