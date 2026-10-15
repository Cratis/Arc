// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class a_recovered_command_scenario : a_generated_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_keep_complete_route_fixtures_and_omit_capped_success_scenarios(bool authoring, bool capped)
    {
        var command = ProductionRouteSources.With($"public object[] Handle() => [new AuthorAudited(Name), {ProductionRouteSources.Wrapper}];");
        const string Scenario = """
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_it_routes
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Task Because() => _scenario.Execute(new RegisterAuthor("account-1", "ledger-2", "2026-10", "Jane"));
                [Fact] Task should_audit() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorAudited>("account-1", e => e.Name == "Jane");
                [Fact] Task should_register() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("ledger-2", e => e.Name == "Jane");
            }
            """;
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = capped ? SemanticVersion.V9 : null },
            (Analyzed.SlicePath, command),
            ("Library/Authors/Registration/when_registering/and_it_routes.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        AssertDocument();
        if (capped)
        {
            Result.Source.ShouldNotContain("specification WhenRegisteringAndItRoutes");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("withheld production route", StringComparison.Ordinal)).ShouldBeTrue();
        }
        else
        {
            Result.Source.ShouldContain("specification WhenRegisteringAndItRoutes");
            Result.Source.ShouldContain("for \"account-1\"");
            Result.Source.ShouldContain("for \"ledger-2\"");
            Result.Source.ShouldContain("streamId = \"2026-10\"");
            Run("WhenRegisteringAndItRoutes").Passed.ShouldBeTrue();
        }
    }
}
