// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.UseEventSourceDefinitionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceDefinitionAnalyzer.when_validating_a_command;

/// <summary>
/// A stream the definition does not declare is not spelled out by it.
/// </summary>
public class and_the_definition_does_not_declare_the_stream : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
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
    [EventSourceType(""Account"")]
    [EventStreamType(""Postings"")]
    public record Deposit(Guid AccountId)
    {
        public FundsDeposited Handle() => new(AccountId);
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
