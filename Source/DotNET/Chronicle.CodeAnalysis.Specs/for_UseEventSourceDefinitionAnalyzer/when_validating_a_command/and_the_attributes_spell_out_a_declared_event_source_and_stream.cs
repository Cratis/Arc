// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.UseEventSourceDefinitionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceDefinitionAnalyzer.when_validating_a_command;

/// <summary>
/// The legacy attributes spell out a definition and one of its streams, so the definition can replace them.
/// </summary>
public class and_the_attributes_spell_out_a_declared_event_source_and_stream : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(
        @"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace TestNamespace
{
    public record FundsDeposited(Guid AccountId);

    [EventSource(Concurrency = ConcurrencyDimensions.EventSourceId)]
    [EventStream(""Transactions"")]
    public class AccountEventSource : IEventSource;

    [Command]
    [{|#0:EventSourceType(""Account"")|}]
    [EventStreamType(""Transactions"")]
    public record Deposit(Guid AccountId)
    {
        public FundsDeposited Handle() => new(AccountId);
    }
}",
        VerifyCS.Diagnostic("ARCCHR0013")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0).WithArguments("Deposit", "Account", "[EventSource<AccountEventSource>(\"Transactions\")]")));

    [Fact] void should_report_the_suggestion() => _result.ShouldBeNull();
}
