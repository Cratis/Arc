// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_CommandSyntaxBuilder.when_building;

/// <summary>
/// Every word a command body dispatches on can still be a property name. The generator keeps each property and lets
/// Screenplay's printer escape the names that would otherwise be read as directives.
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

    [Fact] void should_keep_every_property() => _result.Properties.Select(_ => _.Name).ShouldContainOnly(["authorize", "concurrency", "description", "handler", "produces", "validate", "title"]);
    [Fact] void should_report_every_escaped_property() => _diagnostics.All.Count.ShouldEqual(6);
    [Fact] void should_report_escaped_properties_as_information() => _diagnostics.All.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_report_them_all_under_the_same_code() => _diagnostics.All.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_name_every_escaped_property() => _diagnostics.All.Count(_ => _.Message.Contains("'Validate'", StringComparison.Ordinal)).ShouldEqual(1);
}
