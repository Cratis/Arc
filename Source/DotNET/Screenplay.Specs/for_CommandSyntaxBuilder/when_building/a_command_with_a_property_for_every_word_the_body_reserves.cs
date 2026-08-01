// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_CommandSyntaxBuilder.when_building;

/// <summary>
/// Every word a command body dispatches on takes a property with it. Two of them are worse than a compile error:
/// <c>authorize</c> is read as a reference to a policy and <c>produces</c> as a reference to an event, so a document
/// keeping them says something the application never said and warns about the invention rather than the loss.
/// </summary>
public class a_command_with_a_property_for_every_word_the_body_reserves : given.a_command_syntax_builder
{
    CommandSyntax _result;

    void Because() => _result = _builder.Build(
        new CommandModel(
            "RequestBook",
            null,
            [
                Declare.Property("Authorize", "AuthorizationToken"),
                Declare.Property("Concurrency", "ConcurrencyToken"),
                Declare.Property("Description", "RequestDescription"),
                Declare.Property("Handler", "HandlerName"),
                Declare.Property("Produces", "ProductionKind"),
                Declare.Property("Validate", "ValidationMode"),
                Declare.Property("Title", "BookTitle")
            ],
            null,
            [],
            [],
            null,
            "Lending/Requesting/Requesting.cs"),
        "Library.Lending.Requesting");

    [Fact] void should_keep_only_the_property_no_directive_answers_to() => _result.Properties.Select(_ => _.Name).ShouldContainOnly(["title"]);
    [Fact] void should_report_every_property_it_left_out() => _diagnostics.All.Count.ShouldEqual(6);
    [Fact] void should_report_them_all_under_the_same_code() => _diagnostics.All.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_name_every_property_it_left_out() => _diagnostics.All.Count(_ => _.Message.Contains("'Validate'", StringComparison.Ordinal)).ShouldEqual(1);
}
