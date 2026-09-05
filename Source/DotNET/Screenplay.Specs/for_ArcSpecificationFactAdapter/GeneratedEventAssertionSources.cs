// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

static class GeneratedEventAssertionSources
{
    public const string Assertions = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Events;

        namespace Cratis.Arc.Chronicle.Testing.Commands;

        public static class CommandScenarioChronicleAssertionExtensions
        {
            public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
                this CommandScenario<TCommand> scenario,
                EventSourceId eventSourceId,
                Func<TEvent, bool> predicate) => Task.CompletedTask;
        }
        """;

    public static AdapterContribution Analyze(string scenario)
    {
        var compilation = Analyzed.Compile(
        [
            ("Projects/Registration/RegisterProject/RegisterProject.cs", PlacementScenarioSources.Slice),
            ("Projects/Registration/RegisterProject/when_registering.cs", scenario),
            ("Integration/Assertions.cs", Assertions),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ]);
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        return new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }
}
