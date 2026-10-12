// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_EmittedIdentityCompliance.when_applying;

public class with_a_nested_destination : given.a_document
{
    void Establish()
    {
        Application = Application with
        {
            Types = [new TypeSyntax("Envelope", [new PropertySyntax("value", new TypeRefSyntax("ProtectedValue", false, false, SourceLocation.Start), SourceLocation.Start)], SourceLocation.Start)]
        };
        ChangeCommand(command => command with
        {
            Properties = command.Properties.Select(property => property.Name == "destination" ? property with { Type = new TypeRefSyntax("Envelope", false, false, SourceLocation.Start) } : property).ToList(),
            Produces = command.Produces.Select(production => production with
            {
                For = new PathExpressionSyntax("destination.value", SourceLocation.Start),
                Mappings = [new PropertyMappingSyntax("value", new PathExpressionSyntax("destination.value", SourceLocation.Start), SourceLocation.Start)]
            }).ToList()
        });
    }

    void Because() => Apply();

    [Fact] void should_resolve_the_concept_at_the_end_of_the_path() => AssertStripped("for destination of 'Saved' in command 'Change'");
}
