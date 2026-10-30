// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_stage_shaped_command_identifiers : a_generated_document
{
    [Theory]
    [InlineData("string", "Id", false)]
    [InlineData("string", "(EventSourceId)this.Id", true)]
    [InlineData("Guid", "Id", true)]
    [InlineData("Guid", "new EventSourceId((Id).ToString()!)", false)]
    [InlineData("int", "new EventSourceId((Id).ToString())", true)]
    [InlineData("long", "new EventSourceId(Id.ToString(CultureInfo.InvariantCulture))", false)]
    [InlineData("TextIdentity", "new EventSourceId((Id).ToString())", true)]
    [InlineData("TextIdentity", "new EventSourceId(Id)", false)]
    [InlineData("NumberIdentity", "new EventSourceId((Id).ToString())", false)]
    [InlineData("UuidIdentity", "new EventSourceId((Id).ToString())", true)]
    [InlineData("UuidIdentity", "new EventSourceId((Guid)Id)", false)]
    [InlineData("SourceIdentity", "Id", false)]
    [InlineData("EventSourceId", "Id", true)]
    public void should_preserve_the_proven_property(string type, string identity, bool block)
    {
        Generate((Analyzed.SlicePath, GeneratedIdentifierSources.With(type, identity, block: block)));
        AssertDocument();
        var command = Bound.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Properties.Single(property => property.Name == "id").IsIdentifier.ShouldBeTrue();
        command.Properties.Single(property => property.Name == "otherId").IsIdentifier.ShouldBeFalse();
        Result.Source.ShouldContain("for id");
        Result.Diagnostics.ShouldBeEmpty();
    }
}
