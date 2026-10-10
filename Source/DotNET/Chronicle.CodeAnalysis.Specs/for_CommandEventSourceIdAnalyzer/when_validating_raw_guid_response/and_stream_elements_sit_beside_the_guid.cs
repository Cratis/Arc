// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_stream_elements_sit_beside_the_guid : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Streams
{
    public sealed record CompleteStream(EventStreamType? EventStreamType = default, EventStreamId? EventStreamId = default);
}

namespace TestNamespace
{
    [EventType]
    public record AuthorRegistered(string Name);

    [Command]
    public record RegisterAuthor(string Name)
    {
        public {|#0:(Guid, AuthorRegistered, CompleteStream, EventTags)|} Handle() => (Guid.NewGuid(), new(Name), new(), new EventTags([]));
    }
}",
        VerifyCS.Diagnostic("ARCCHR0010").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("RegisterAuthor", "AuthorRegistered")));

    [Fact] void should_report_only_the_event() => _result.ShouldBeNull();
}
