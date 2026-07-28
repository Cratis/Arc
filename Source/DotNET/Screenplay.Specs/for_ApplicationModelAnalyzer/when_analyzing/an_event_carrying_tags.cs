// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Tags classify an event and are the one piece of event metadata Screenplay has a counterpart for. They can be
/// written in either shape and more than once, so all of them are collected and ordered.
/// </summary>
public class an_event_carrying_tags : Specification
{
    const string Source = """
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        [Tag("audit")]
        [Tags("authors", "audit")]
        public record AuthorRegistered(string Name, int Age);
        """;

    ApplicationModelAnalysis _analysis;
    EventModel _event;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _event = _analysis.Slice().Events.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_name_the_event() => _event.Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_recover_its_properties() => _event.Properties.Select(_ => _.Name).ShouldContainOnly(["Name", "Age"]);
    [Fact] void should_collect_every_tag_once() => _event.Tags.ShouldContainOnly(["audit", "authors"]);
    [Fact] void should_order_the_tags() => _event.Tags.First().ShouldEqual("audit");
    [Fact] void should_infer_a_state_view_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.StateView);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
