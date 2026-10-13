// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Reactors;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ReactorAppendRouting.when_classifying_results;

public class with_mixed_or_unreadable_handlers : Specification
{
    [Theory]
    [InlineData("public Welcomed Another(Registered @event) => new();", true)]
    [InlineData("public void Another(Registered @event) { }", false)]
    [InlineData("public object Another(Registered @event) { if (@event is not null) return new Welcomed(); return new object(); }", true)]
    [InlineData("public abstract object Another(Registered @event);", true)]
    public void should_keep_context_routing_if_any_handler_may_need_it(string otherHandler, bool routes)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.EventSequences;
            namespace Library;
            [EventType] public record Registered;
            [EventType] public record Welcomed;
            public abstract class Welcomer
            {
                public EventForEventSourceId Welcome(Registered @event) => new("author", new Welcomed());
                {{otherHandler}}
            }
            """));
        var handlers = compilation.GetTypeByMetadataName("Library.Welcomer")!.GetMembers().OfType<IMethodSymbol>()
            .Where(method => method.MethodKind == MethodKind.Ordinary).ToList();
        var routing = new ReactorAppendRouting(new SemanticModels([compilation]));

        routing.UsesContextRoute(handlers).ShouldEqual(routes);
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
    }
}
