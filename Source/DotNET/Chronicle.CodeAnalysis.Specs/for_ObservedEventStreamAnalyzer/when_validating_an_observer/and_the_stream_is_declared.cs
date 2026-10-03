// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ObservedEventStreamAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ObservedEventStreamAnalyzer.when_validating_an_observer;

/// <summary>
/// A stream the definition declares is a valid selector.
/// </summary>
public class and_the_stream_is_declared : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(
        @"
using System.Threading.Tasks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

namespace TestNamespace
{
    [EventType]
    public record FundsDeposited(decimal Amount);

    [EventSource]
    [EventStream(""Transactions"")]
    public class AccountEventSource : IEventSource;

    [FromEventSource<AccountEventSource>(""Transactions"")]
    public class Notifier : IReactor
    {
        public Task Deposited(FundsDeposited @event, EventContext context) => Task.CompletedTask;
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
