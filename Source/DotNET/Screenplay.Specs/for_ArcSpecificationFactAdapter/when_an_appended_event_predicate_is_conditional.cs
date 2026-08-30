// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_an_appended_event_predicate_is_conditional : Specification
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_registering;

        public class when_registering
        {
            readonly CommandScenario<RegisterProject> _scenario = new();

            async Task Because() => await _scenario.Execute(new RegisterProject("Screenplay"));

            [Fact] Task should_append()
            {
                var compareName = true;
                return _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(
                    "project",
                    @event => compareName ? @event.Name == "Screenplay" : @event.Name == "Other");
            }
        }
        """;

    AdapterContribution _contribution = null!;

    void Because() => _contribution = GeneratedEventAssertionSources.Analyze(Scenario);

    [Fact] void should_contribute_no_partial_facts() => _contribution.Facts.ShouldBeEmpty();
    [Fact] void should_report_only_the_unsupported_scenario() => _contribution.Diagnostics.Select(_ => _.Code).ShouldContainOnly("ARCSP0001");
}
