// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

static class PlacementScenarioSources
{
    public const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Projects.Projects.Registration.RegisterProject;

        [EventType]
        public record ProjectRegistered(string Name);

        [Command]
        public record RegisterProject(string Name)
        {
            public ProjectRegistered Handle() => new(Name);
        }
        """;

    public const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_rejecting;

        public class when_rejecting
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterProject(""));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;
}
