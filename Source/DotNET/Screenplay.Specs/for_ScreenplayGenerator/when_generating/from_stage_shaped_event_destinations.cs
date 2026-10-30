// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_stage_shaped_event_destinations : a_production_route_document
{
    [Theory]
    [InlineData("string", "Id", "new EventSourceId((OtherId).ToString())")]
    [InlineData("Guid", "Id", "new EventSourceId((OtherId).ToString()!)")]
    [InlineData("int", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("long", "new EventSourceId(Id.ToString())", "new EventSourceId(OtherId.ToString(CultureInfo.InvariantCulture))")]
    [InlineData("TextIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("NumberIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("UuidIdentity", "new EventSourceId(Id.ToString())", "new EventSourceId((OtherId).ToString())")]
    [InlineData("SourceIdentity", "Id", "OtherId")]
    public void should_preserve_the_exact_destination(string type, string identity, string destination)
    {
        Generate((Analyzed.SlicePath, GeneratedIdentifierSources.With(type, identity, destination)));
        AssertRoutedDocument();
        Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().Destination.ShouldEqual(new PropertyPathSource("OtherId"));
        var command = Bound.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var resolved = command.Produces.Single().Destination as SemanticResolvedExpression;
        resolved.ShouldNotBeNull();
        resolved!.Target.ShouldEqual(command.Properties.Single(property => property.Name == "otherId").Id);
        Result.Diagnostics.ShouldBeEmpty();
    }
}
