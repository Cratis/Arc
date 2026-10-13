// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_an_unreadable_constraint : a_returned_event_batch
{
    [Theory]
    [InlineData("{ builder.Unique<AuthorRegistered>(); Configure(builder); }", "static void Configure(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.Unique<AuthorFiled>();")]
    [InlineData("{ builder.Unique<AuthorRegistered>(); Configure(builder); }", "static void Configure(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.Unique(unique => unique.On(new Cratis.Chronicle.Events.EventType(\"filed\", 1), [\"Country\"]));")]
    [InlineData("=> builder.Unique(unique => unique.On<AuthorRegistered>(e => e.Country).RemovedWith(new Cratis.Chronicle.Events.EventType(\"filed\", 1)));", "")]
    public void should_withhold_only_batches(string definition, string helper)
    {
        var constraint = $$"""
            public class FilingConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) {{definition}}
                {{helper}}
            }
            public class Greeter : IReactor
            {
                public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
            }
            """;
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", constraint);

        AssertWithheld();
        Result.Source.ShouldContain("produces AuthorWelcomed");
        if (definition.Contains("RemovedWith", StringComparison.Ordinal))
        {
            Result.Source.ShouldNotContain("constraint ");
            Result.Diagnostics.Single(_ => _.Code == "SP0017").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        }

        Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("cannot be read");
    }
}
