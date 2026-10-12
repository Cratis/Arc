// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ProtectedIdentityAnnotations.when_applying;

public class with_route_mapping_positions : given.a_document
{
    [Theory]
    [InlineData("selected", false)]
    [InlineData("candidate", false)]
    [InlineData("production", false)]
    [InlineData("selected", true)]
    [InlineData("candidate", true)]
    [InlineData("production", true)]
    public void should_find_the_protected_source_property(string position, bool part)
    {
        var type = new TypeRefSyntax("ProtectedValue", false, false, SourceLocation.Start);
        var identity = new TypeRefSyntax("String", false, false, SourceLocation.Start);
        Application = Application with
        {
            EventSources = [new EventSourceSyntax("Account", SourceLocation.Start)
            {
                Identifier = identity,
                Streams = [new EventStreamSyntax("Transactions", SourceLocation.Start)
                {
                    StreamId = part ? null : type,
                    StreamIdParts = part ? [new EventStreamIdPartSyntax("owner", type, SourceLocation.Start), new EventStreamIdPartSyntax("region", identity, SourceLocation.Start)] : []
                }]
            }]
        };
        var mapping = new PropertyMappingSyntax(part ? "owner" : "streamId", new PathExpressionSyntax("destination", SourceLocation.Start), SourceLocation.Start);
        var route = new CommandStreamSyntax("Account", "Transactions", SourceLocation.Start)
        {
            StreamId = part ? null : mapping,
            StreamIdParts = part ? [mapping, new PropertyMappingSyntax("region", new PathExpressionSyntax("id", SourceLocation.Start), SourceLocation.Start)] : []
        };
        ChangeCommand(command => command with
        {
            Stream = position == "selected" || position == "candidate" ? route : null,
            StreamCandidates = position == "candidate" ? [route] : [],
            Produces = command.Produces.Select(production => production with { Stream = position == "production" ? route : null }).ToList()
        });

        Apply();

        // Duplicate route candidates are deliberately non-compiling ambiguity evidence.
        AssertStripped(part ? "stream id part 'owner' route mapping" : "stream id route mapping", compiles: position != "candidate");
    }
}
