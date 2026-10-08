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
    public void should_keep_legacy_productions_but_omit_unrepresentable_scenario_sources(string handler, bool responseAssertion)
    {
        GenerateScenario(handler, responseAssertion ? UnstatableResponseAssertion : null);
        Result.Source.ShouldNotContain("specification WhenRegisteringAndANameIsSupplied");
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("for authorId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification &&
            diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning &&
            diagnostic.Message.Contains("event sources cannot be stated faithfully", StringComparison.Ordinal)).ShouldBeTrue();
        if (!handler.Contains("ToUpperInvariant", StringComparison.Ordinal))
        {
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("withheld to keep successful scenarios", StringComparison.Ordinal)).ShouldBeTrue();
        }
        AssertDocument();
    }

    [Fact]
    public void should_withhold_generation_even_when_the_scenario_compares_the_generated_response_with_a_literal()
    {
        GenerateScenario(
            "public (AuthorId, AuthorRegistered) Handle() { var authorId = new AuthorId(Guid.NewGuid()); return (authorId, new(Name)); }",
            "((CommandResult<AuthorId>)_result).Response.ShouldEqual(new AuthorId(Guid.Parse(\"6f3c8b47-1938-4d4c-8f26-817e306a10e2\")))");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("no seam a scenario can pin", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Assert.Equal(\"Apollo\", ((CommandResult<string>)_result).Response)")]
    public void should_keep_a_response_the_scenario_states_or_does_not_assert(string? assertion)
    {
        GenerateScenario("public (AuthorRegistered, string) Handle() => (new(Name), Name);", assertion);
        Result.Source.ShouldNotContain("specification WhenRegisteringAndANameIsSupplied");
        Result.Source.ShouldContain("returns name");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeFalse();
        AssertDocument();
    }

    [Fact]
    public void should_omit_a_response_assertion_scenario_only_when_the_emitted_command_has_returns()
    {
        GenerateScenario("public (AuthorRegistered, string) Handle() => (new(Name), Name);", UnstatableResponseAssertion);
        var emitted = new ScreenplayEmitter().Emit(Result.Model, new() { AuthoringOnlyConstructs = true });
        emitted.Source.ShouldContain("returns name");
        emitted.Source.ShouldNotContain("specification WhenRegisteringAndANameIsSupplied");
        var diagnostic = emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Message.ShouldContain("CommandResult.Response");
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual("Library.Authors.Registration.when_registering.and_a_name_is_supplied");
    }

    const string UnstatableResponseAssertion = "((CommandResult<string>)_result).Response.ShouldNotBeNull()";

    void GenerateScenario(string handler, string? responseAssertion)
    {
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal);
        var scenario = $$"""
                using System;
                using System.Threading.Tasks;
                using Cratis.Arc.Commands;
                using Cratis.Arc.Testing.Commands;
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Chronicle.Testing.EventSequences;
                using Cratis.Specifications;
                using Library.Authors.Registration;
                using Xunit;
                namespace Library.Authors.Registration.when_registering;
                public class and_a_name_is_supplied
                {
                    readonly CommandScenario<RegisterAuthor> _scenario = new();
                    Cratis.Arc.Commands.CommandResult _result = null!;
                    async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Apollo"));
                    [Fact] Task should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "Apollo");
                    {{(responseAssertion is null ? string.Empty : $"[Fact] void should_return_the_name() => {responseAssertion};")}}
                }
                """;
        Generate(
            (Analyzed.SlicePath, IdentifierSources.With("[Command] public record RegisterAuthor(string Name) { " + handler + " }")),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", scenario));
    }
}
