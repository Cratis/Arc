// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_wrapped_event_without_a_response : a_production_route_document
{
    void Because() => Generate((Analyzed.SlicePath, WrappedEventSources.Command));

    [Fact] void should_keep_the_production_and_destination() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().Destination.ShouldEqual(new PropertyPathSource("OtherId"));
    [Fact] void should_not_infer_a_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_not_report_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertRoutedDocument();
}
