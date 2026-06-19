// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_multiple_implicit_conversion_ids_without_interface : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    public record AuthorId(string Value)
    {
        public static implicit operator EventSourceId(AuthorId id) => new EventSourceId(id.Value);
    }

    [Command]
    public record RegisterAuthor(AuthorId First, AuthorId Second);
}",
                VerifyCS.Diagnostic("ARCCHR0002")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithArguments("RegisterAuthor")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
