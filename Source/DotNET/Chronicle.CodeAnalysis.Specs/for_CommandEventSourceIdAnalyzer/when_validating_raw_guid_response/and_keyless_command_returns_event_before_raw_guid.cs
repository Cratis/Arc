// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_keyless_command_returns_event_before_raw_guid : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    [EventType]
    public record ExpenseSubmitted(decimal Amount);

    [Command]
    public record SubmitExpense(decimal Amount)
    {
        public {|#0:(ExpenseSubmitted, Guid)|} Handle() => (new ExpenseSubmitted(Amount), Guid.NewGuid());
    }
}",
        VerifyCS.Diagnostic("ARCCHR0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("SubmitExpense", "ExpenseSubmitted")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
