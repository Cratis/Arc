// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_CommandSyntaxBuilder.when_building;

/// <summary>
/// Only the words a command body actually dispatches on cost a property, so a name that merely reads like a keyword
/// elsewhere in the language - <c>tag</c> belongs to an event body, <c>key</c> to a projection - stays exactly where
/// it is. Leaving those out would drop members the document can perfectly well describe.
/// </summary>
public class a_command_with_properties_no_directive_answers_to : given.a_command_syntax_builder
{
    CommandSyntax _result;

    void Because() => _result = _builder.Build(
        new CommandModel(
            "RequestBook",
            null,
            [
                Declare.Property("Title", "BookTitle"),
                Declare.Property("Tag", "RequestTag"),
                Declare.Property("Key", "RequestKey"),
                Declare.Property("Parent", "RequestId"),
                Declare.Property("Events", "EventKind")
            ],
            null,
            [],
            [],
            null,
            "Lending/Requesting/Requesting.cs"),
        "Library.Lending.Requesting");

    [Fact] void should_keep_every_property() => _result.Properties.Select(_ => _.Name).ShouldContainOnly(["title", "tag", "key", "parent", "events"]);
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
