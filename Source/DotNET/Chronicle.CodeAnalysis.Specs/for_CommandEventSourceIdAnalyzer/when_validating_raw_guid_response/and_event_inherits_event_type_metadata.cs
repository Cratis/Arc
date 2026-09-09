// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_event_inherits_event_type_metadata : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        [EventType] public abstract record ExpenseEvent;
        public abstract record ExpenseSubmission : ExpenseEvent;
        public record ExpenseSubmitted : ExpenseSubmission;

        [Command] public record SubmitExpense
        {
            public {|#0:(Guid, ExpenseSubmitted)|} Handle() => (Guid.NewGuid(), new());
        }
        ",
        VerifyCS.Diagnostic("ARCCHR0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("SubmitExpense", "ExpenseSubmitted")));

    [Fact] void should_report_the_derived_event() => _result.ShouldBeNull();
}
