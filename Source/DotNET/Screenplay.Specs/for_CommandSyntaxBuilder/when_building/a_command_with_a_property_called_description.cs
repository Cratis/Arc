// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_CommandSyntaxBuilder.when_building;

/// <summary>
/// A command body reads the first word of every line to decide what the line is, so a property called
/// <c>Description</c> is written as <c>description RequestDescription</c> and read as the description of the command
/// - which is a document that does not compile. The language offers nothing to escape the name with, so the property
/// is left out and the loss is reported.
/// </summary>
public class a_command_with_a_property_called_description : given.a_command_syntax_builder
{
    CommandSyntax _result;

    void Because() => _result = _builder.Build(
        new CommandModel(
            "RequestBook",
            null,
            [
                Declare.Property("Title", "BookTitle"),
                Declare.Property("Description", "RequestDescription")
            ],
            null,
            [],
            [],
            null,
            "Lending/Requesting/Requesting.cs"),
        "Library.Lending.Requesting");

    [Fact] void should_leave_the_property_out() => _result.Properties.Select(_ => _.Name).ShouldNotContain("description");
    [Fact] void should_keep_the_properties_it_can_write() => _result.Properties.Select(_ => _.Name).ShouldContainOnly(["title"]);
    [Fact] void should_report_the_property() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_report_it_as_a_warning() => _diagnostics.All.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_locate_the_report_where_the_command_lives() => _diagnostics.All.Single().Location.ShouldEqual("Library.Lending.Requesting");
    [Fact] void should_name_the_property_in_the_report() => _diagnostics.All.Single().Message.Contains("'Description'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_declaring_type_in_the_report() => _diagnostics.All.Single().Message.Contains("'RequestBook'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_block_reserving_it_in_the_report() => _diagnostics.All.Single().Message.Contains("command block", StringComparison.Ordinal).ShouldBeTrue();
}
