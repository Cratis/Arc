// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_collection_element_inherits_event_type_metadata : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        [EventType] public abstract record ExpenseEvent;
        public record ExpenseSubmitted : ExpenseEvent;

        [Command] public record SubmitExpenseArray
        {
            public {|#0:(Guid, ExpenseSubmitted[])|} Handle() => (Guid.NewGuid(), [new()]);
        }

        [Command] public record SubmitExpenseEnumerable
        {
            public {|#1:(Guid, IEnumerable<ExpenseSubmitted>)|} Handle() => (Guid.NewGuid(), new ExpenseSubmitted[] { new() });
        }

        [Command] public record SubmitExpenseList
        {
            public {|#2:(Guid, List<ExpenseSubmitted>)|} Handle() => (Guid.NewGuid(), [new()]);
        }
        ",
        VerifyCS.Diagnostic("ARCCHR0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("SubmitExpenseArray", "ExpenseSubmitted"),
        VerifyCS.Diagnostic("ARCCHR0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(1)
            .WithArguments("SubmitExpenseEnumerable", "ExpenseSubmitted"),
        VerifyCS.Diagnostic("ARCCHR0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(2)
            .WithArguments("SubmitExpenseList", "ExpenseSubmitted")));

    [Fact] void should_report_the_derived_event_in_each_collection_shape() => _result.ShouldBeNull();
}
