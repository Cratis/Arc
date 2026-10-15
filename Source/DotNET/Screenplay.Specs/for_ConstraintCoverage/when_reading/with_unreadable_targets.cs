// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ConstraintCoverage.when_reading;

public class with_unreadable_targets
{
    [Theory]
    [InlineData("=> builder.Unique(unique => unique.On(new Cratis.Chronicle.Events.EventType(\"filed\", 1), [\"Name\"]));")]
    [InlineData("=> builder.Unique(unique => unique.On<E>(e => e.Name).RemovedWith(new Cratis.Chronicle.Events.EventType(\"filed\", 1)));")]
    [InlineData("=> builder.Unique(Configure);", "static void Configure(Cratis.Chronicle.Events.Constraints.IUniqueConstraintBuilder unique) => unique.On<E>(e => e.Name);")]
    public void should_fail_closed_for_unknown_event_targets(string body, string helper = "")
    {
        var source = $$"""
            using Cratis.Chronicle.Events.Constraints;
            namespace Library;
            public record E(string Name);
            public class Rule : IConstraint
            {
                public void Define(IConstraintBuilder builder) {{body}}
                {{helper}}
            }
            """;
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var rule = compilation.GetTypeByMetadataName("Library.Rule")!;
        var @event = compilation.GetTypeByMetadataName("Library.E")!;
        var coverage = ConstraintCoverage.Read([rule, @event], new SemanticModels([compilation]));

        var gap = coverage.UnprovenBatch([@event, @event]);
        gap!.Message.ShouldContain("constraint 'Rule' cannot be read");
        gap.Kind.ShouldEqual(ReactionBatchGapKind.Unreadable);
    }
}
