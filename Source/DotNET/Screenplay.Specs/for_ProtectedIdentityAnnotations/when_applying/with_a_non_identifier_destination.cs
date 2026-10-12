// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ProtectedIdentityAnnotations.when_applying;

public class with_a_non_identifier_destination : given.a_document
{
    void Establish() => ChangeCommand(command => command with
    {
        Produces = command.Produces.Select(production => production with { For = new PathExpressionSyntax("destination", SourceLocation.Start) }).ToList()
    });

    void Because() => Apply();

    [Fact] void should_name_the_destination_and_produce_a_compiling_document() => AssertStripped("for destination of 'Saved' in command 'Change'");
}
