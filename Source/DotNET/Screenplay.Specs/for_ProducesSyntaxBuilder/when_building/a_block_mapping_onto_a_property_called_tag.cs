// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ProducesSyntaxBuilder.when_building;

/// <summary>
/// A produces block fills in the properties of the event it produces, and dispatches on <c>tag</c> exactly as the
/// event body does. A mapping onto a property called <c>Tag</c> is therefore written as <c>tag = tag</c> and read as
/// a tag whose value is an assignment, which the compiler rejects outright.
/// </summary>
public class a_block_mapping_onto_a_property_called_tag : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ProducesSyntaxBuilder _builder;
    IEnumerable<ProducesSyntax> _result;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _diagnostics = new();
        _builder = new(naming, new NameAvailability(naming, _diagnostics));
    }

    void Because() => _result = _builder.Build(
        [
            new ProducesModel(
                "BookRequested",
                null,
                [
                    Declare.From("Title", "Title"),
                    Declare.From("Tag", "Tag")
                ])
        ],
        "Library.Lending.Requesting");

    [Fact] void should_leave_the_mapping_out() => _result.Single().Mappings.Select(_ => _.Property).ShouldContainOnly(["title"]);
    [Fact] void should_report_the_mapping() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_locate_the_report_where_the_command_lives() => _diagnostics.All.Single().Location.ShouldEqual("Library.Lending.Requesting");
    [Fact] void should_name_the_event_the_property_belongs_to() => _diagnostics.All.Single().Message.Contains("'BookRequested'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_block_reserving_it_in_the_report() => _diagnostics.All.Single().Message.Contains("produces block", StringComparison.Ordinal).ShouldBeTrue();
}
