// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_EventSyntaxBuilder.when_building;

/// <summary>
/// An event body dispatches on <c>tag</c>, so a property called <c>Tag</c> is written as <c>tag BookTag</c> and read
/// as a tag on the event. That is worse than a compile error - a tag naming a type is accepted, so the property
/// disappears and a tag the application never declared appears in its place, silently.
/// </summary>
public class an_event_with_a_property_called_tag : given.an_event_syntax_builder
{
    EventSyntax _result;

    void Because() => _result = _builder.Build(
        new EventModel(
            "BookRequested",
            [
                Declare.Property("Title", "BookTitle"),
                Declare.Property("Tag", "BookTag")
            ],
            ["audit"]),
        "Library.Lending.Requesting");

    [Fact] void should_leave_the_property_out() => _result.Properties.Select(_ => _.Name).ShouldContainOnly(["title"]);
    [Fact] void should_not_turn_the_property_into_a_tag() => _result.Tags!.Count().ShouldEqual(1);
    [Fact] void should_report_the_property() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_locate_the_report_where_the_event_lives() => _diagnostics.All.Single().Location.ShouldEqual("Library.Lending.Requesting");
    [Fact] void should_name_the_property_in_the_report() => _diagnostics.All.Single().Message.Contains("'Tag'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_declaring_type_in_the_report() => _diagnostics.All.Single().Message.Contains("'BookRequested'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_block_reserving_it_in_the_report() => _diagnostics.All.Single().Message.Contains("event block", StringComparison.Ordinal).ShouldBeTrue();
}
