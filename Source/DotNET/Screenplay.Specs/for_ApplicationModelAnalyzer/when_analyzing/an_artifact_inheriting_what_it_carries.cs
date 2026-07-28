// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A property declared on a base type is part of what the artifact carries just as much as one declared on the type
/// itself - it is serialized, it is sent, and it is what a caller has to fill in. Reading only what the type itself
/// declares would describe a shape that does not exist, and say nothing about the difference.
/// </summary>
public class an_artifact_inheriting_what_it_carries : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        public abstract record Audited
        {
            public string CorrelationId { get; init; } = string.Empty;
        }

        [EventType]
        public record AuthorRegistered(string Name) : Audited;

        [Command]
        public record RegisterAuthor(string Name) : Audited
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_carry_the_inherited_property_on_the_event() => _analysis.Slice().Events.Single().Properties.Select(_ => _.Name).ShouldContainOnly(["CorrelationId", "Name"]);
    [Fact] void should_carry_the_inherited_property_on_the_command() => _analysis.Slice().Commands.Single().Properties.Select(_ => _.Name).ShouldContainOnly(["CorrelationId", "Name"]);
    [Fact] void should_declare_the_inherited_property_first() => _analysis.Slice().Events.Single().Properties.First().Name.ShouldEqual("CorrelationId");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
