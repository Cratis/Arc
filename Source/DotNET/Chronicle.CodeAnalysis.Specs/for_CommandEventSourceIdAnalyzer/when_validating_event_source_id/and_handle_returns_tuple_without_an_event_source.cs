// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_handle_returns_tuple_without_an_event_source : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    public record RegistrationResponse(string Status);

    [Command]
    public record {|#0:RegisterAuthor|}(EventSourceId First, EventSourceId Second)
    {
        public (RegistrationResponse, RegistrationResponse) Handle() =>
            (new RegistrationResponse(""a""), new RegistrationResponse(""b""));
    }
}",
                VerifyCS.Diagnostic("ARCCHR0002")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("RegisterAuthor")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
