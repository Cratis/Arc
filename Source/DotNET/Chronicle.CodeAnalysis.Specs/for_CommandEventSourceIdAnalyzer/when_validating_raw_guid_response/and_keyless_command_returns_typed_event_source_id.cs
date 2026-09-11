// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_keyless_command_returns_typed_event_source_id : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    public record ExpenseId(Guid Value) : EventSourceId<Guid>(Value);

    [EventType]
    public record ExpenseSubmitted(decimal Amount);

    [Command]
    public record SubmitExpense(decimal Amount)
    {
        public (ExpenseSubmitted, ExpenseId) Handle() => (new ExpenseSubmitted(Amount), new ExpenseId(Guid.NewGuid()));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
