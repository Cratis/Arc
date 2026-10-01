// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture intentionally concatenates raw source snippets.
#pragma warning disable SA1117 // Snippet arguments and expected diagnostics are displayed separately.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_analyzing_command_decisions
{
    const string HandlerAdvice = "Mark the command [ProtectedDecision] and use DecisionRead<T> or IDecisionReads in Provide or Handle, or mark the command or the intentional legacy read [Unprotected]";
    const string ValidatorAdvice = "Protected commands refuse validators with constructor dependencies, so move the read into Provide or Handle as DecisionRead<T> under [ProtectedDecision], or mark the command [Unprotected]";

    const string Definitions = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Arc.Validation;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Monads;
        using FluentValidation;
        using OneOf;
        [EventType("28cae753-4d3c-4e7b-8b24-d60c2f81a906")]
        public record Created;
        [FromEvent<Created>]
        public record State([property: Key] Guid Id);
        """;

    const string ReadModelsWrapper = """
        public record OwnerId(Guid Value) : EventSourceId<Guid>(Value);
        public static class ReadModelsExtensions
        {
            public static Task<T> GetInstanceById<T>(this IReadModels readModels, EventSourceId id, ReadModelSessionId? sessionId = null) =>
                readModels.GetInstanceById<T>((ReadModelKey)id, sessionId);
        }
        """;

    [Fact]
    public async Task plain_chronicle_model_in_event_returning_handle_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle(State state) => new();
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task legacy_read_in_provide_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<State> Provide(IReadModels models) => await models.GetInstanceById<State>(new ReadModelKey("a"));
                public Created Handle(State state) => new();
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info), AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task plain_model_with_aggregate_mutation_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            public class SampleAggregate : AggregateRoot;
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public void Handle(State state, SampleAggregate aggregate) { }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task protected_read_followed_by_immediate_append_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<Created> Handle(DecisionRead<State> read, IEventLog log)
                {
                    await log.Append(EventSourceId, new Created());
                    return new Created();
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012")
                .WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task void_returning_handler_with_decision_and_store_append_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task Handle(DecisionRead<State> read, IEventStore store)
                {
                    await store.EventLog.Append(EventSourceId, new Created());
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012")
                .WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task event_array_returning_handler_with_plain_model_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created[] Handle(State state) => [new Created()];
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task validator_plain_model_and_explicit_legacy_read_report_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle() => new();
            }
            public class CreateValidator : CommandValidator<Create>
            {
                public CreateValidator(State state, IReadModels models)
                {
                    _ = models.GetInstanceById<State>(new ReadModelKey("key"));
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info),
            AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task validator_read_advises_moving_it_out_of_the_validator() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle() => new();
            }
            public class CreateValidator(State state) : CommandValidator<Create>;
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", ValidatorAdvice));

    [Fact]
    public async Task task_of_result_returning_handle_reports_legacy_reads() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<Result<EventsWithConcurrencyScopes, string>> Handle(IReadModels models)
                {
                    _ = await models.GetInstanceById<State>(new ReadModelKey("key"));
                    return new EventsWithConcurrencyScopes([], []);
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task nested_one_of_and_value_task_returning_handle_reports_plain_model() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public ValueTask<OneOf<Created, string>> Handle(State state) => default;
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task task_of_result_of_untyped_event_array_reports_plain_model() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Task<Result<object[], string>> Handle(State state) => default!;
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task task_of_result_without_events_is_not_reported() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Get(EventSourceId EventSourceId)
            {
                public Task<Result<State, string>> Handle(State state) => default!;
            }
            """);

    [Fact]
    public async Task validator_rule_lambda_legacy_read_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId, string Name)
            {
                public Created Handle() => new();
            }
            public class CreateValidator : CommandValidator<Create>
            {
                public CreateValidator(IReadModels readModels)
                {
                    RuleFor(_ => _.Name).MustAsync(async (name, token) => (await readModels.GetInstanceById<State>(new ReadModelKey("key"))) is not null);
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", ValidatorAdvice));

    [Fact]
    public async Task validator_whole_command_rule_lambda_legacy_read_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId, Guid OtherId)
            {
                public Created Handle() => new();
            }
            public class CreateValidator : CommandValidator<Create>
            {
                public CreateValidator(IReadModels readModels)
                {
                    RuleFor(_ => _)
                        .MustAsync(async (command, token) =>
                            await readModels.GetInstanceById<State>(new ReadModelKey(command.OtherId.ToString())) is { } state && state.Id == command.OtherId);
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", ValidatorAdvice));

    [Fact]
    public async Task enumerable_of_event_for_event_source_id_returning_handle_reports_plain_model() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public IEnumerable<EventForEventSourceId> Handle(State state) => [new(EventSourceId, new Created())];
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task task_of_enumerable_of_event_for_event_source_id_with_provide_reports_legacy_read() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<State?> Provide(IReadModels models) => await models.GetInstanceById<State>(new ReadModelKey("key"));
                public Task<IReadOnlyList<EventForEventSourceId>> Handle(State? state) => default!;
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info), AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task validator_whole_command_rule_read_through_extension_wrapper_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + ReadModelsWrapper + """
            [Command]
            public record Create(OwnerId OwnerId)
            {
                public Created Handle() => new();
            }
            public class CreateValidator : CommandValidator<Create>
            {
                public CreateValidator(IReadModels readModels)
                {
                    RuleFor(_ => _)
                        .MustAsync(async (command, token) => await readModels.GetInstanceById<State>(command.OwnerId) is not null);
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", ValidatorAdvice));

    [Fact]
    public async Task provide_read_through_extension_wrapper_reports_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + ReadModelsWrapper + """
            [Command]
            public record Create(OwnerId OwnerId)
            {
                public async Task<State?> Provide(IReadModels readModels) => await readModels.GetInstanceById<State>(OwnerId);
                public IEnumerable<EventForEventSourceId> Handle() => [new(OwnerId, new Created())];
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "State", HandlerAdvice));

    [Fact]
    public async Task children_from_declared_on_record_parameter_makes_plain_model_report_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [EventType("5a3f0c14-77d1-4a3e-9d0a-0f3c7a1b2e64")]
            public record ItemAdded(Guid ItemId);
            public record Item(Guid ItemId);
            public record Roster(
                [property: Key] Guid Id,
                [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ItemId), identifiedBy: nameof(Item.ItemId))] IEnumerable<Item> Items);
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public IEnumerable<EventForEventSourceId> Handle(Roster? roster) => [new(EventSourceId, new Created())];
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("Create", "Roster", HandlerAdvice));

    [Fact]
    public async Task model_without_projection_attributes_is_not_reported_for_extension_wrapper_reads() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + ReadModelsWrapper + """
            public record Unrelated(Guid Id);
            [Command]
            public record Get(OwnerId OwnerId)
            {
                public State Handle(State state) => state;
            }
            public static class Helpers
            {
                public static Task<Unrelated> GetInstanceById(this Unrelated source, EventSourceId id) => Task.FromResult(source);
            }
            [Command]
            public record Create(OwnerId OwnerId)
            {
                public async Task<Created> Handle(Unrelated source)
                {
                    _ = await source.GetInstanceById(OwnerId);
                    return new Created();
                }
            }
            """);

    [Fact]
    public async Task validator_helper_and_field_initializer_reads_report_info() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId, string Name)
            {
                public Created Handle() => new();
            }
            public class CreateValidator(IReadModels readModels) : CommandValidator<Create>
            {
                readonly System.Func<Task<State>> _read = () => readModels.GetInstanceById<State>(new ReadModelKey("key"));

                public async Task<bool> Exists() => await readModels.GetInstanceById<State>(new ReadModelKey("key")) is not null;
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info),
            AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task lambda_and_non_constructor_validator_parameters_are_not_reported() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId, State Snapshot)
            {
                public Created Handle() => new();
            }
            public class CreateValidator : CommandValidator<Create>
            {
                public CreateValidator()
                {
                    System.Func<State, bool> check = (State state) => state is not null;
                    Use(state => state is not null);
                    bool Local(State state) => state is not null;
                }

                public static bool Check(State state) => state is not null;

                static void Use(System.Func<State, bool> check) { }
            }
            """);

    [Fact]
    public async Task unprotected_parameter_is_acknowledged_without_disabling_other_legacy_reads() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle([Unprotected] State state, IReadModels models)
                {
                    _ = models.GetInstanceById<State>(new ReadModelKey("key"));
                    return new Created();
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));

    [Fact]
    public async Task unprotected_command_and_transactional_append_are_not_reported() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            [Unprotected]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<Created> Handle(State state, IEventLog log)
                {
                    await log.Append(EventSourceId, new Created());
                    return new Created();
                }
            }
            [Command]
            public record Protected(EventSourceId EventSourceId)
            {
                public async Task<Created> Handle(DecisionRead<State> read, IEventLog log)
                {
                    await log.Transactional.Append(EventSourceId, new Created());
                    return new Created();
                }
            }
            """);

    [Fact]
    public async Task read_only_command_and_non_chronicle_parameter_are_not_reported() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            public record OtherState;
            [Command]
            public record Get(EventSourceId EventSourceId)
            {
                public State Handle(State state) => state;
            }
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle(OtherState state) => new();
            }
            """);
}
