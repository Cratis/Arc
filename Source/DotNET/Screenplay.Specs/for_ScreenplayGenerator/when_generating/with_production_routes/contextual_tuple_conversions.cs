// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class contextual_tuple_conversions : a_production_route_document
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_follow_the_bound_tuple_identity_type_instead_of_guessing_from_a_raw_guid(bool authoring, bool identity)
    {
        var type = identity ? "EventSourceId" : "Guid";
        var source = ProductionRouteSources.With($"public ({type}, AuthorRegistered) Handle() => (OtherId, new(Name));", properties: "[Key] Guid Id, Guid OtherId, string Month, string Name");
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, source));
        Result.Source.ShouldContain(identity ? "for otherId" : "for id");
        Result.Source.ShouldNotContain(identity ? "for id" : "for otherId");
        AssertRoutedDocument();
    }
}
