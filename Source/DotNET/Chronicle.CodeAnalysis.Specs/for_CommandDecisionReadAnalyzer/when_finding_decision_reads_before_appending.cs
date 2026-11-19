// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Compiler source fixtures are independent of platform line endings.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_finding_decision_reads_before_appending : Specification
{
    [Theory]
    [InlineData("public State Provide(DecisionRead<State> read) => default!;")]
    [InlineData("public void Helper(DecisionRead<State> read) { }")]
    public Task should_find_decision_parameters_on_other_command_methods(string member) => Verify($$"""
        [Command] public record Change(OwnerId Owner)
        {
            {{member}}
            public async Task<Changed> Handle(IEventLog log)
            {
                await log.Append(Owner, new Changed());
                return new();
            }
        }
        """);

    [Fact]
    public Task should_find_decision_parameters_on_the_enclosing_validator() => Verify("""
        [Command] public record Change { public Changed Handle() => new(); }
        public class Validator : CommandValidator<Change>
        {
            public Validator(IEventLog log)
            {
                _ = log.Append(new OwnerId(Guid.Empty), new Changed());
            }
            public void Helper(DecisionRead<State> read) { }
        }
        """);

    [Fact]
    public Task should_find_a_decision_reader_invocation_without_a_decision_parameter() => Verify("""
        [Command] public record Change(OwnerId Owner)
        {
            public async Task<Changed> Handle(Func<IDecisionReads> factory, IEventLog log)
            {
                _ = await factory().Get<State>(new ReadModelKey("key"));
                await log.Append(Owner, new Changed());
                return new();
            }
        }
        """);

    [Fact]
    public Task should_report_an_explicit_decision_command_without_a_read() => Verify("""
        [Command, Cratis.Arc.Chronicle.ReadModels.ProtectedDecision] public record Change(OwnerId Owner)
        {
            public async Task Handle(IEventLog log) => await log.Append(Owner, new Changed());
        }
        """);

    static Task Verify(string declaration) => AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(
        $$"""
        using Cratis.Arc.Commands;
        using Cratis.Chronicle.EventSequences;
        {{DecisionAnalyzerSource.Definitions}}
        {{declaration}}
        """,
        AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012")
            .WithSeverity(DiagnosticSeverity.Info));
}
