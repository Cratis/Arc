// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ProtectedIdentityAnnotations.when_applying;

public class with_a_property_candidate_route : given.a_document
{
    void Establish() => ChangeCommand(command => command with
    {
        Stream = new CommandStreamSyntax("Account", "Transactions", SourceLocation.Start)
        {
            PropertyCandidate = command.Properties.Single(property => property.Name == "destination"),
            StreamId = new PropertyMappingSyntax("streamId", new PathExpressionSyntax("destination", SourceLocation.Start), SourceLocation.Start)
        }
    });

    void Because() => Apply();

    [Fact] void should_keep_both_protection_annotations() => Result.Concepts.Single().Attributes.Count().ShouldEqual(2);
    [Fact] void should_not_claim_an_unselected_route_is_a_protected_position() => Diagnostics.All.ShouldBeEmpty();
}
