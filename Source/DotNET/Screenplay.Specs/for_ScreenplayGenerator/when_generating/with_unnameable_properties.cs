// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_unnameable_properties : given.a_generated_document
{
    [Theory]
    [InlineData("IDictionary<string, int>")]
    [InlineData("KeyValuePair<string, int>")]
    [InlineData("IRule")]
    [InlineData("Rule")]
    [InlineData("IReadOnlyList<IRule>")]
    [InlineData("IReadOnlyList<IDictionary<string, int>>")]
    public void should_omit_the_property_without_an_unknown_type(string type)
    {
        Generate((Analyzed.SlicePath, $$"""
            using System.Collections.Generic;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public interface IRule;
            public abstract class Rule;
            public record Detail(string Label, {{type}} Unsupported);
            [EventType] public record AuthorRegistered(string Name, {{type}} Unsupported, Detail Detail);
            [Command] public record InspectAuthor(string Name, {{type}} Unsupported)
            {
                public void Handle() { }
            }
            """));

        Result.Source.ShouldContain("event AuthorRegistered");
        Result.Source.ShouldContain("command InspectAuthor");
        Result.Source.ShouldContain("type Detail");
        Result.Source.ShouldNotContain("unsupported ");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference && diagnostic.Message.Contains("AuthorRegistered.Unsupported", StringComparison.Ordinal));
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference && diagnostic.Message.Contains("Detail.Unsupported", StringComparison.Ordinal));
        AssertDocument();
    }

    [Fact]
    public void should_bind_a_carried_record_whose_only_property_is_unnameable()
    {
        Generate((Analyzed.SlicePath, """
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public interface IRule;
            public record Detail(IRule Rule);
            [EventType] public record AuthorRegistered(Detail Detail);
            """));

        Result.Source.ShouldNotContain("rule ");
        AssertDocument();
    }

    [Fact]
    public void should_omit_a_read_model_emptied_by_property_omission_and_its_query()
    {
        Generate((Analyzed.SlicePath, """
            using System.Collections.Generic;
            using Cratis.Arc.Queries.ModelBound;
            namespace Library.Authors.Registration;
            public interface IRule;
            [ReadModel] public record Report(IRule Rule)
            {
                public static IEnumerable<Report> All() => [];
            }
            """));

        Result.Source.ShouldNotContain("readmodel Report");
        Result.Source.ShouldNotContain("query All");
        Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference && diagnostic.Message.Contains("Report.Rule", StringComparison.Ordinal));
        AssertDocument();
    }

    [Fact]
    public void should_bind_an_event_whose_only_property_is_unnameable()
    {
        Generate((Analyzed.SlicePath, """
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            public interface IRule;
            [EventType] public record AuthorRegistered(IRule Rule);
            """));

        Result.Source.ShouldContain("event AuthorRegistered");
        Result.Source.ShouldNotContain("rule ");
        AssertDocument();
    }
}
