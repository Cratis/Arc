// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceIdForRawGuidResponseCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceIdForRawGuidResponseCodeFixProvider;

public class when_rewriting_the_raw_guid_response
{
    const string Source = @"
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
        public {|#0:(Guid, ExpenseSubmitted)|} Handle()
        {
            var expenseId = Guid.NewGuid();
            return (expenseId, new ExpenseSubmitted(Amount));
        }
    }
}";

    const string FixedSource = @"
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
        public (global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>, ExpenseSubmitted) Handle()
        {
            var expenseId = Guid.NewGuid();
            return (expenseId, new ExpenseSubmitted(Amount));
        }
    }
}";

    [Fact] async Task should_use_a_typed_event_source_id() => await VerifyCS.VerifyCodeFixAsync(
        Source,
        FixedSource,
        new ExpectedDiagnostic("ARCCHR0010", DiagnosticSeverity.Warning, "SubmitExpense", "ExpenseSubmitted"));
}
