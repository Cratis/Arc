// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_EmittedIdentityCompliance.when_applying;

public class with_event_source_identity_positions : given.a_document
{
    [Theory]
    [InlineData("identifier")]
    [InlineData("stream id")]
    [InlineData("stream id part")]
    public void should_withhold_protection_only_for_the_declared_position(string position)
    {
        var type = new TypeRefSyntax("ProtectedValue", false, false, SourceLocation.Start);
        Application = Application with
        {
            EventSources = [new EventSourceSyntax("Account", SourceLocation.Start)
            {
                Identifier = position == "identifier" ? type : null,
                Streams = [new EventStreamSyntax("Transactions", SourceLocation.Start)
                {
                    StreamId = position == "stream id" ? type : null,
                    StreamIdParts = position == "stream id part" ? [new EventStreamIdPartSyntax("owner", type, SourceLocation.Start), new EventStreamIdPartSyntax("region", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)] : []
                }]
            }]
        };

        Apply();

        AssertStripped(position == "identifier" ? "identifier of event source 'Account'" : position == "stream id" ? "stream id of 'Account.Transactions'" : "stream id part 'owner' of 'Account.Transactions'");
    }
}
