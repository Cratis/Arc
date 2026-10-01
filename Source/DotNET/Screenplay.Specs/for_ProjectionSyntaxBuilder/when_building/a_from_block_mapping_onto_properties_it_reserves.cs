// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.for_ProjectionSyntaxBuilder.when_building;

/// <summary>
/// A projection <c>from</c> block dispatches on <c>key</c> and <c>parent</c>, so assignments onto read model properties
/// of either name must be escaped. The generator keeps those mappings and reports the escape. Mappings that lead with
/// an operation remain unchanged.
/// </summary>
public class a_from_block_mapping_onto_properties_it_reserves : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ProjectionSyntaxBuilder _builder;
    ProjectionSyntax? _result;

    void Establish()
    {
        var naming = new ScreenplayNaming();
        _diagnostics = new();
        _builder = new(naming, _diagnostics, new NameAvailability(naming, _diagnostics));
    }

    void Because() => _result = _builder.Build(
        new ProjectionModel(
            "Library.Lending.Listing.RequestProjection",
            "Request",
            "event-log",
            ProjectionAutoMapMode.Enabled,
            false,
            ProjectionScopeModel.Empty with
            {
                From =
                [
                    new(
                        ["BookRequested"],
                        "$eventSourceId",
                        null,
                        Declare.Map(
                            ("title", "title"),
                            ("key", "requestKey"),
                            ("parent", "parentId"),
                            ("count", ProjectionMappingConverter.Increment)))
                ]
            }),
        "Library.Lending.Listing");

    IEnumerable<string> Filled => _result!.Blocks.OfType<FromSyntax>().Single().Mappings.Select(_ => _.Property);

    [Fact] void should_keep_the_assignments_the_block_reserves() => Filled.ShouldContainOnly(["count", "key", "parent", "title"]);
    [Fact] void should_report_escaped_mappings_as_information() => _diagnostics.All.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_keep_a_mapping_leading_with_its_operation() => _result!.Blocks.OfType<FromSyntax>().Single().Mappings.OfType<IncrementMappingSyntax>().Single().Property.ShouldEqual("count");
    [Fact] void should_report_both_properties() => _diagnostics.All.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.NameReservedByGrammar, ScreenplayDiagnosticCodes.NameReservedByGrammar]);
    [Fact] void should_locate_the_reports_where_the_projection_lives() => _diagnostics.All.Select(_ => _.Location).Distinct().ShouldContainOnly(["Library.Lending.Listing"]);
    [Fact] void should_name_the_read_model_in_the_reports() => _diagnostics.All.Count(_ => _.Message.Contains("'Request'", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_name_the_block_reserving_them_in_the_reports() => _diagnostics.All.Count(_ => _.Message.Contains("from block", StringComparison.Ordinal)).ShouldEqual(2);
}
