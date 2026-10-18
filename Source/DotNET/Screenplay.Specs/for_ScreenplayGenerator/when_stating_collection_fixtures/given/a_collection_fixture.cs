// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_collection_fixtures.given;

public class a_collection_fixture : for_ScreenplayGenerator.given.a_generated_document
{
    protected void GenerateFixture(string elementType, string expression)
    {
        var slice = $$"""
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
            public record ApprovalRequested(IEnumerable<{{elementType}}> Tags);

            [Command]
            public record RequestApproval([Key] string Id, IEnumerable<{{elementType}}> Tags)
            {
                public ApprovalRequested Handle() => new(Tags);
            }
            """;
        var scenario = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Library.Approvals.Requesting;
            using Xunit;

            namespace Library.Approvals.Requesting.when_requesting;

            public class and_tags_are_stated
            {
                const string Urgent = "urgent";
                readonly CommandScenario<RequestApproval> _scenario = new();
                Result _result = null!;

                void Establish() => _scenario.Given.ForEventSource("approval-1").Events(new ApprovalRequested({{expression}}));

                async Task Because() => _result = await _scenario.Execute(new RequestApproval("approval-1", {{expression}}));

                [Fact] void should_reject() => _result.ShouldHaveValidationErrors();

                static string Computed() => Guid.NewGuid().ToString();
                static T[] Empty<T>() => [default!];
            }
            """;
        Generate(
            ("Library/Approvals/Requesting/Requesting.cs", slice),
            ("Library/Approvals/Requesting/when_requesting/and_tags_are_stated.cs", scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
    }

    protected void AssertTags(string text)
    {
        Assert.True(Result.Source.Split($"tags = {text}", StringSplitOptions.None).Length == 3,
            $"{Result.Source}{Environment.NewLine}{string.Join(Environment.NewLine, Result.Diagnostics)}");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        AssertDocument();
    }

    protected void AssertUnread()
    {
        Result.Source.ShouldNotContain("specification WhenRequestingAndTagsAreStated");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        AssertDocument();
    }
}
