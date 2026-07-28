// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// The projection definition language rejects host expressions, so a mapping the language cannot express has to be
/// left out. It is reported rather than dropped, which is what turns silent loss into something a reader can act on.
/// </summary>
public class a_projection_with_an_unmappable_expression : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with
        {
            Slices =
            [
                LibraryAuthors.Registration(),
                LibraryAuthors.Listing() with { Projection = ProjectionWithAnUnmappableMapping() }
            ]
        };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_keep_the_mapping_it_can_express() => _emission.Source.Contains("name = name", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_mapping_it_cannot_express_out() => _emission.Source.Contains("summary", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_mapping_as_unmappable() => _emission.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableProjectionExpression]);
    [Fact] void should_name_the_expression_in_the_report() => _emission.Diagnostics.Single().Message.Contains("$env.SERVICE", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_compile() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_still_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();

    static ProjectionModel ProjectionWithAnUnmappableMapping() =>
        new(
            "Library.Authors.Listing.AuthorProjection",
            "Author",
            "event-log",
            ProjectionAutoMapMode.Enabled,
            false,
            ProjectionScopeModel.Empty with
            {
                From =
                [
                    new(
                        ["AuthorRegistered"],
                        "$eventSourceId",
                        null,
                        Declare.Map(("name", "name"), ("summary", "$env.SERVICE")))
                ]
            });
}
