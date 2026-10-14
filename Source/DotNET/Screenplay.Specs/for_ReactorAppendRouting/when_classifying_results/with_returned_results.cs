// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Reactors;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ReactorAppendRouting.when_classifying_results;

public class with_returned_results : Specification
{
    [Theory]
    [InlineData("EventForEventSourceId", "new(\"author\", new Welcomed())", false)]
    [InlineData("Task<EventForEventSourceId>", "Task.FromResult(new EventForEventSourceId(\"author\", new Welcomed()))", false)]
    [InlineData("ValueTask<EventForEventSourceId>", "new(new EventForEventSourceId(\"author\", new Welcomed()))", false)]
    [InlineData("IEnumerable<EventForEventSourceId>", "[new(\"author\", new Welcomed())]", false)]
    [InlineData("EventForEventSourceId[]", "[new(\"author\", new Welcomed())]", false)]
    [InlineData("IEnumerable<object>", "[new EventForEventSourceId(\"author\", new Welcomed())]", false)]
    [InlineData("object", "new EventForEventSourceId(\"author\", new Welcomed())", false)]
    [InlineData("Task<object>", "Task.FromResult<object>(new EventForEventSourceId(\"author\", new Welcomed()))", false)]
    [InlineData("Task<SendWelcome>", "Task.FromResult(new SendWelcome())", false)]
    [InlineData("IEnumerable<object>", "[new SendWelcome()]", false)]
    [InlineData("IEnumerable<object>", "[]", false)]
    [InlineData("Task", "Task.CompletedTask", false)]
    [InlineData("ValueTask", "ValueTask.CompletedTask", false)]
    [InlineData("Welcomed", "new()", true)]
    [InlineData("Welcomed[]", "[new()]", true)]
    [InlineData("Task<Welcomed>", "Task.FromResult(new Welcomed())", true)]
    [InlineData("IEnumerable<object>", "[new Welcomed()]", true)]
    [InlineData("IEnumerable<object>", "[new EventForEventSourceId(\"author\", new Welcomed()), new Welcomed()]", true)]
    [InlineData("IEnumerable<object>", "[new SendWelcome(), new Welcomed()]", true)]
    [InlineData("object", "GetResult()", true)]
    public void should_use_the_context_route_only_for_bare_or_unproven_results(string returnType, string expression, bool routes)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            namespace Library;
            [EventType] public record Registered;
            [EventType] public record Welcomed;
            [Command] public record SendWelcome;
            public class Welcomer
            {
                public {{returnType}} Welcome(Registered @event) => {{expression}};
                static object GetResult() => new Welcomed();
            }
            """));
        var handler = compilation.GetTypeByMetadataName("Library.Welcomer")!.GetMembers("Welcome").OfType<IMethodSymbol>().Single();
        var routing = new ReactorAppendRouting(new SemanticModels([compilation]));

        routing.UsesContextRoute([handler]).ShouldEqual(routes);
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
    }
}
