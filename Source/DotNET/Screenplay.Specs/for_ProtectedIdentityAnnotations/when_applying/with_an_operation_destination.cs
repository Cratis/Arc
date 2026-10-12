// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ProtectedIdentityAnnotations.when_applying;

public class with_an_operation_destination : given.a_document
{
    void Establish() => ChangeCommand(command => command with
    {
        Produces = command.Produces.Select(production => production with
        {
            For = new PathExpressionSyntax("destination", SourceLocation.Start),
            InlineOperation = new OperationSyntax("Save", "Storage", [], SourceLocation.Start)
        }).ToList()
    });

    void Because() => Apply();

    [Fact] void should_keep_both_protection_annotations() => Result.Concepts.Single().Attributes.Count().ShouldEqual(2);
    [Fact] void should_not_treat_an_operation_as_an_event_production() => Diagnostics.All.ShouldBeEmpty();
}
