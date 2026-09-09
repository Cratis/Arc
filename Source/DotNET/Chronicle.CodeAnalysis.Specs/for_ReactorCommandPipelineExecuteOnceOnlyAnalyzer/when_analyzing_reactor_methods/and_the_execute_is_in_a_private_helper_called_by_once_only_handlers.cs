// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// The exact shape reported as a false positive in #2615: two [OnceOnly] handlers share a private helper whose
/// own first parameter is not an event type, so the helper is never itself a dispatch candidate — only the
/// [OnceOnly] handlers that reach it matter, and both are already excused.
/// </summary>
public class and_the_execute_is_in_a_private_helper_called_by_once_only_handlers : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

namespace TestNamespace
{
    [EventType]
    public record DigestOpened(string Payload);
    [EventType]
    public record DigestDismissed(string Payload);
    public record WeeklyDigestContent(string Payload);
    public record RecordDigestOutcome(string Payload);

    public class DigestReactor(ICommandPipeline commandPipeline) : IReactor
    {
        [OnceOnly]
        public Task On(DigestOpened @event, EventContext context) =>
            Analyze(new WeeklyDigestContent(@event.Payload), context);

        [OnceOnly]
        public Task On(DigestDismissed @event, EventContext context) =>
            Analyze(new WeeklyDigestContent(@event.Payload), context);

        Task Analyze(WeeklyDigestContent content, EventContext context) =>
            commandPipeline.Execute(new RecordDigestOutcome(content.Payload));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
