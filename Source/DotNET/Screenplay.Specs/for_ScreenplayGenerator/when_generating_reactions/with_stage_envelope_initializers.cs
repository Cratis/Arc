// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_stage_envelope_initializers : a_filtered_application
{
    [Theory]
    [InlineData("Occurred = context.Occurred")]
    [InlineData("Tags = [\"x\"]")]
    [InlineData("Occurred = context.Occurred, Tags = [\"x\"]")]
    public void should_recover_metadata_already_represented_by_the_event(string initializer)
    {
        GenerateEnvelope(initializer);
        AssertDocument();

        Result.Source.ShouldContain("produces Greeting");
        Result.Source.ShouldContain("tag x");
        Result.Source.ShouldContain("for country");
        Result.Source.ShouldContain("name = name");
        Result.Source.ShouldContain("greetedAt = $context.occurred");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("requirementId");
        Result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Subject = \"s\"", true)]
    [InlineData("Tags = BuildTags()", true)]
    [InlineData("Occurred = DateTimeOffset.UtcNow", true)]
    [InlineData("Tags = [\"x\", \"extra\"]", true)]
    [InlineData("Tags = [\"other\"]", true)]
    [InlineData("Occurred = context.Occurred", false)]
    [InlineData("Occurred = context.Occurred, Tags = [\"other\"]", true)]
    [InlineData("Occurred = context.Occurred, Subject = \"s\"", true)]
    [InlineData("Tags = [.. BuildTags()]", true)]
    [InlineData("Tags = [TagName]", true)]
    [InlineData("Tags = [$\"x\"]", true)]
    public void should_keep_unrepresented_metadata_as_code(string initializer, bool mapsOccurred)
    {
        GenerateEnvelope(initializer, mapsOccurred);

        Result.Source.ShouldNotContain("produces Greeting");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData("[\"x\", \"y\"]", true)]
    [InlineData("[\"y\", \"x\"]", false)]
    [InlineData("[\"x\"]", false)]
    [InlineData("[\"x\", \"y\", \"y\"]", false)]
    [InlineData("[\"X\", \"y\"]", false)]
    public void should_require_the_exact_ordered_event_tags(string tags, bool recovered)
    {
        GenerateEnvelope($"Tags = {tags}", eventTags: "[Cratis.Chronicle.Tags(\"y\", \"x\")]");

        Result.Source.Contains("produces Greeting", StringComparison.Ordinal).ShouldEqual(recovered);
        Result.Source.Contains("file Authors/Welcoming/Welcoming.cs", StringComparison.Ordinal).ShouldEqual(!recovered);
        Result.Diagnostics.ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("[Cratis.Chronicle.Tag(\"x\")]", false)]
    public void should_recover_empty_tags_only_for_an_untagged_event(string eventTags, bool recovered)
    {
        GenerateEnvelope("Tags = []", eventTags: eventTags);

        Result.Source.Contains("produces Greeting", StringComparison.Ordinal).ShouldEqual(recovered);
        Result.Source.Contains("file Authors/Welcoming/Welcoming.cs", StringComparison.Ordinal).ShouldEqual(!recovered);
        Result.Diagnostics.ShouldBeEmpty();
        AssertDocument();
    }

    [Fact]
    public void should_not_require_an_occurrence_mapping_for_tags_alone()
    {
        GenerateEnvelope("Tags = [\"x\"]", mapsOccurred: false);

        Result.Source.ShouldContain("produces Greeting");
        Result.Source.ShouldContain("for country");
        Result.Source.ShouldContain("name = name");
        Result.Source.ShouldContain("tag x");
        Result.Source.ShouldNotContain("greetedAt =");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("requirementId");
        Result.Diagnostics.ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData("task", true)]
    [InlineData("collection", true)]
    [InlineData("mixed", false)]
    public void should_prove_every_envelope_in_the_returned_shape(string shape, bool recovered)
    {
        GenerateEnvelope("Occurred = context.Occurred, Tags = [\"x\"]", shape: shape);

        Result.Source.Contains("produces Greeting", StringComparison.Ordinal).ShouldEqual(recovered);
        Result.Source.Contains("file Authors/Welcoming/Welcoming.cs", StringComparison.Ordinal).ShouldEqual(!recovered);
        if (recovered)
        {
            Result.Source.ShouldContain("for country");
            Result.Source.ShouldContain("name = name");
            Result.Source.ShouldContain("greetedAt = $context.occurred");
            Result.Source.ShouldNotContain("requirementId");
        }
        Result.Diagnostics.ShouldBeEmpty();
        AssertDocument();
    }

    void GenerateEnvelope(string initializer, bool mapsOccurred = true, string eventTags = "[Cratis.Chronicle.Tag(\"x\")]", string shape = "single")
    {
        const string Wrapper = "global::Cratis.Chronicle.EventSequences.EventForEventSourceId";
        var payload = $"new Greeting(@event.Name{(mapsOccurred ? ", context.Occurred" : string.Empty)})";
        var envelope = $"new {Wrapper}(@event.Country, {payload}) {{ {initializer} }}";
        var (returnType, returned) = shape switch
        {
            "task" => ($"Task<{Wrapper}>", $"Task.FromResult({envelope})"),
            "collection" => ($"IEnumerable<{Wrapper}>", $"[{envelope}]"),
            "mixed" => ($"IEnumerable<{Wrapper}>", $"[{envelope}, new {Wrapper}(@event.Country, {payload}) {{ Subject = \"s\" }}]"),
            _ => (Wrapper, envelope)
        };
        GenerateFilteredWith($$"""
        [EventType]
        {{eventTags}}
        public record Greeting(string Name{{(mapsOccurred ? ", DateTimeOffset GreetedAt" : string.Empty)}});

        public class Welcomer : IReactor
        {
            const string TagName = "x";

            public {{returnType}} Welcome(AuthorRegistered @event, EventContext context) => {{returned}};

            static string[] BuildTags() => ["x"];
        }
        """);
    }
}
