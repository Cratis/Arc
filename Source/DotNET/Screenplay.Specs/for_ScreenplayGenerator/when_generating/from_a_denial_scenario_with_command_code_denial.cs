// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario_with_command_code_denial : a_generated_document
{
    [Theory]
    [InlineData("public Cratis.Arc.Authorization.AuthorizationResult Provide() => Cratis.Arc.Authorization.AuthorizationResult.Failure(\"not allowed\"); public AuthorRegistered Handle() => new(Name);")]
    [InlineData("public System.Threading.Tasks.Task<Cratis.Arc.Authorization.AuthorizationResult> Provide() => System.Threading.Tasks.Task.FromResult(Cratis.Arc.Authorization.AuthorizationResult.Failure(\"not allowed\")); public AuthorRegistered Handle() => new(Name);")]
    [InlineData("public Cratis.Arc.Authorization.AuthorizationResult Handle() => Cratis.Arc.Authorization.AuthorizationResult.Failure(\"not allowed\");")]
    [InlineData("public Cratis.Arc.Commands.CommandResult Handle() => Cratis.Arc.Commands.CommandResult.Unauthorized(default);")]
    [InlineData("public object Handle() => Cratis.Arc.Authorization.AuthorizationResult.Failure(\"not allowed\");")]
    [InlineData("public IOutcome Handle() => ReadOutcome(); static IOutcome ReadOutcome() => null!; public interface IOutcome { }")]
    [InlineData("public AuthorRegistered Handle() => new(Name); static Cratis.Arc.Authorization.AuthorizationResult Deny() => Cratis.Arc.Authorization.AuthorizationResult.Failure(\"not allowed\");")]
    public void should_withhold_a_denial_that_command_code_can_supply(string code)
    {
        var sources = DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"))
            .Select(source => (source.Path, source.Text.Replace("public AuthorRegistered Handle() => new(Name);", code, StringComparison.Ordinal))).ToArray();
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("command code can return an authorization denial");
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
