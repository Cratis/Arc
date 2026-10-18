// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_event_collection_predicates.given;

public class an_event_collection_predicate : for_ScreenplayGenerator.given.a_generated_document
{
    const string Slice = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Concepts;

        namespace Library.Approvals.Requesting;

        public record ApprovalLabel(string Value) : ConceptAs<string>(Value)
        {
            public static implicit operator ApprovalLabel(string value) => new(value);
        }

        [EventType]
        public record ApprovalRequested(string Requester, IEnumerable<ApprovalLabel> Tags);

        [Command]
        public record RequestApproval([Key] string Id, IEnumerable<ApprovalLabel> Tags)
        {
            public ApprovalRequested Handle() => new("Jane Austen", Tags);
        }
        """;

    const string Assertions = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Events;

        namespace Cratis.Arc.Chronicle.Testing.Commands;

        public static class CommandScenarioChronicleAssertionExtensions
        {
            public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
                this CommandScenario<TCommand> scenario, EventSourceId eventSourceId, Func<TEvent, bool> predicate) => Task.CompletedTask;
        }
        """;

    protected void GenerateExpectation(string predicate, string fixture = "[Urgent, \"review\"]")
    {
        var scenario = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Library.Approvals.Requesting;
            using Xunit;

            namespace Library.Approvals.Requesting.when_requesting;

            public class and_the_event_matches
            {
                const string Urgent = "urgent";
                readonly CommandScenario<RequestApproval> _scenario = new();

                Task Because() => _scenario.Execute(new RequestApproval("approval-1", {{fixture}}));

                [Fact] Task should_append() => _scenario.ShouldHaveAppendedEvent<RequestApproval, ApprovalRequested>(
                    "approval-1", e => e.Requester == "Jane Austen" && {{predicate}});

                static string Computed() => Guid.NewGuid().ToString();
            }
            """;
        Generate(
            ("Library/Approvals/Requesting/Requesting.cs", Slice),
            ("Library/Approvals/Requesting/when_requesting/and_the_event_matches.cs", scenario),
            ("Library/Testing/Assertions.cs", Assertions),
            (IntegrationTesting.Path, IntegrationTesting.Source));
    }

    protected void AssertCollection(string text)
    {
        Result.Source.ShouldContain("then ApprovalRequested");
        Result.Source.ShouldContain("requester = \"Jane Austen\"");
        Result.Source.Split($"tags = {text}", StringSplitOptions.None).Length.ShouldEqual(3);
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        AssertDocument();
    }

    protected void AssertUnread()
    {
        Result.Source.ShouldNotContain("specification WhenRequestingAndTheEventMatches");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        AssertDocument();
    }
}
