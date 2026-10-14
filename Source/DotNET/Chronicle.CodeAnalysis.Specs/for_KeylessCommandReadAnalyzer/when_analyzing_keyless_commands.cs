// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture concatenates independently readable snippets.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_KeylessCommandReadAnalyzer;

public class when_analyzing_keyless_commands
{
    [Theory]
    [InlineData("State")]
    [InlineData("State?")]
    public async Task should_warn_for_a_model_with_either_nullability(string model) =>
        await Verify($"[Command] public record Change {{ public Changed Handle({model} state) => new(); }}", Warning());

    [Fact]
    public async Task should_explain_the_newly_generated_lookup_id() =>
        await Verify("[Command] public record Change { public Changed Handle(State state) => new(); }",
            Warning().WithArguments("Command 'Change' has no event source key; read model 'State' is looked up by a newly generated id on every call and will never be found. Add a key property or implement ICanProvideEventSourceId."));

    [Theory]
    [InlineData("IReadModels")]
    [InlineData("IEventStore")]
    public async Task should_advise_for_an_explicit_lookup_dependency(string dependency) =>
        await Verify($"[Command] public record Change {{ public Changed Handle({dependency} state) => new(); }}", Info());

    [Fact]
    public async Task should_warn_for_a_provide_parameter() =>
        await Verify("[Command] public record Change { public Plain Provide(State state) => new(state.Id); public Changed Handle(Plain plain) => new(); }", Warning());

    [Fact]
    public async Task should_not_warn_for_a_model_supplied_by_provide() =>
        await Verify("[Command] public record Change { public State Provide() => new(new OwnerId(Guid.NewGuid())); public Changed Handle(State state) => new(); }");

    [Theory]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task should_not_warn_for_a_model_supplied_by_non_public_provide(string accessibility) =>
        await Verify($"[Command] public record Change {{ {accessibility} State Provide() => new(new OwnerId(Guid.NewGuid())); public Changed Handle(State state) => new(); }}");

    [Theory]
    [InlineData("protected")]
    [InlineData("internal")]
    public async Task should_not_warn_for_a_model_supplied_by_inherited_non_public_provide(string accessibility) =>
        await Verify($"public record Base {{ {accessibility} State Provide() => new(new OwnerId(Guid.NewGuid())); }} [Command] public record Change : Base {{ public Changed Handle(State state) => new(); }}");

    [Fact]
    public async Task should_warn_despite_an_inherited_private_provide() =>
        await Verify("public record Base { private State Provide() => new(new OwnerId(Guid.NewGuid())); } [Command] public record Change : Base { public Changed Handle(State state) => new(); }", Warning());

    [Fact]
    public async Task should_warn_despite_a_static_provide() =>
        await Verify("[Command] public record Change { public static State Provide() => new(new OwnerId(Guid.NewGuid())); public Changed Handle(State state) => new(); }", Warning());

    [Fact]
    public async Task should_warn_for_a_non_public_provide_parameter() =>
        await Verify("[Command] public record Change { private Plain Provide(State state) => new(state.Id); public Changed Handle(Plain plain) => new(); }", Warning());

    [Theory]
    [InlineData("OwnerId Id")]
    [InlineData("EventSourceId Id")]
    [InlineData("[Key] Guid Id")]
    [InlineData("[property: Key] Guid Id")]
    public async Task should_not_warn_for_runtime_key_properties(string property) =>
        await Verify($"[Command] public record Change({property}) {{ public Changed Handle(State state) => new(); }}");

    [Fact]
    public async Task should_not_warn_for_a_custom_key_provider() =>
        await Verify("[Command] public record Change : ICanProvideEventSourceId { public EventSourceId GetEventSourceId() => EventSourceId.New(); public Changed Handle(State state) => new(); }");

    [Fact]
    public async Task should_not_warn_for_an_inherited_key() =>
        await Verify("public record Base(OwnerId Id); [Command] public record Change(OwnerId Id) : Base(Id) { public Changed Handle(State state) => new(); }");

    [Fact]
    public async Task should_not_report_non_event_commands_or_unrelated_dependencies() =>
        await Verify("[Command] public record Query { public State Handle(State state) => state; } [Command] public record Change { public Changed Handle(Plain state) => new(); } public record Other { public Changed Handle(State state) => new(); }");

    [Fact]
    public async Task should_warn_for_awaited_event_collections() =>
        await Verify("[Command] public record Change { public Task<Changed[]> Handle(State state) => Task.FromResult(new[] { new Changed() }); }", Warning());

    [Fact]
    public async Task should_ignore_non_public_and_static_handlers() =>
        await Verify("[Command] public record Change { Changed Handle(State state) => new(); } [Command] public record Other { public static Changed Handle(State state) => new(); }");

    static DiagnosticBuilder Warning() => AnalyzerVerifier<KeylessCommandReadAnalyzer>.Diagnostic("ARCCHR0025").WithSeverity(DiagnosticSeverity.Warning);
    static DiagnosticBuilder Info() => AnalyzerVerifier<KeylessCommandReadAnalyzer>.Diagnostic("ARCCHR0025").WithSeverity(DiagnosticSeverity.Info);
    static Task Verify(string source, params ExpectedDiagnostic[] diagnostics) =>
        AnalyzerVerifier<KeylessCommandReadAnalyzer>.VerifyAnalyzerAsync(DecisionAnalyzerSource.Definitions + source, diagnostics);
}
