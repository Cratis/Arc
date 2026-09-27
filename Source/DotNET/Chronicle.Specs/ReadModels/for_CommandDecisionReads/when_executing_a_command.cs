// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_executing_a_command
{
    [Fact]
    public async Task should_reuse_protected_reads_across_handle_provide_and_validator_and_enroll_each_resolution()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var token = DecisionFixtures.Protected<Model>("source");
        inner.GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>()).Returns(token);
        var reader = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        CommandTransaction.Current = unit;
        CommandDecisionReads.Begin(typeof(Command));
        try
        {
            var handle = await reader.Get<Model>((ReadModelKey)"source");
            var provide = await reader.Get<Model>((ReadModelKey)"source");
            var validator = await reader.Get<Model>((ReadModelKey)"source");
            Assert.Same(handle, provide);
            Assert.Same(handle, validator);
            CommandDecisionReads.VerifyProvided(provide);
            await inner.Received(1).GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>());
            Assert.True(unit.HasEnrolledDecisionReads);
            Assert.Throws<ProtectedUnitOfWorkRequiresOwner>(() => unit.Commit().GetAwaiter().GetResult());
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [Fact]
    public async Task should_enroll_explicit_keys_and_keep_nested_invocations_separate()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var first = DecisionFixtures.Protected<Model>("other");
        var nested = DecisionFixtures.Protected<Model>("other");
        inner.GetDetached<Model>((ReadModelKey)"other", Arg.Any<CancellationToken>()).Returns(first, nested);
        var reader = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        CommandTransaction.Current = unit;
        CommandDecisionReads.Begin(typeof(Command));
        try
        {
            Assert.Same(first, await reader.Get<Model>((ReadModelKey)"other"));
            CommandDecisionReads.Begin(typeof(NestedCommand));
            try
            {
                Assert.Same(nested, await reader.Get<Model>((ReadModelKey)"other"));
                Assert.Throws<InvalidOperationException>(() => CommandDecisionReads.VerifyProvided(first));
            }
            finally
            {
                CommandDecisionReads.End();
            }
            Assert.Same(first, await reader.Get<Model>((ReadModelKey)"other"));
            Assert.True(unit.HasEnrolledDecisionReads);
            await inner.Received(2).GetDetached<Model>((ReadModelKey)"other", Arg.Any<CancellationToken>());
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [Fact]
    public async Task should_enroll_again_when_a_supplied_provider_resolves_the_same_invocation()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var token = DecisionFixtures.Protected<Model>("source");
        inner.GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>()).Returns(token);
        var firstProvider = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        var secondProvider = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        CommandTransaction.Current = unit;
        CommandDecisionReads.Begin(typeof(Command));
        try
        {
            Assert.Same(await firstProvider.Get<Model>((ReadModelKey)"source"), await secondProvider.Get<Model>((ReadModelKey)"source"));
            await inner.Received(1).GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>());
            Assert.True(unit.HasEnrolledDecisionReads);
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [Fact]
    public async Task should_refuse_a_custom_unit_of_work_before_any_protected_fold()
    {
        var (store, _, _) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var reader = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        CommandTransaction.Current = Substitute.For<IUnitOfWork>();
        CommandDecisionReads.Begin(typeof(Command));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Get<Model>((ReadModelKey)"source"));
            await inner.DidNotReceiveWithAnyArgs().GetDetached<Model>((ReadModelKey)"source");
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [Fact]
    public async Task should_commit_a_protected_no_event_command_with_decision_scopes()
    {
        var (store, log, unit) = DecisionFixtures.Transaction();
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<CorrelationId>()).Returns(unit);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IUnitOfWorkManager)).Returns(manager);
        var scope = new TransactionalCommandScope();
        var correlation = CorrelationId.New();
        var context = new CommandContext(correlation, typeof(Command), new Command(), [], new(), ServiceProvider: services);
        IDictionary<EventSourceId, ConcurrencyScope>? captured = null;
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call =>
            {
                Assert.Empty(call.ArgAt<IEnumerable<EventForEventSourceId>>(0));
                captured = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                return AppendManyResult.Success(correlation, []);
            });
        scope.Begin(context);
        unit.AddDecisionRead(DecisionFixtures.Protected<Model>("source"));
        var result = CommandResult.Success(correlation);
        await scope.Complete(context, result);
        Assert.True(result.IsSuccess);
        Assert.True(unit.IsCompleted);
        Assert.NotNull(captured);
        Assert.Contains((EventSourceId)"source", captured.Keys);
        Assert.Equal(CommandCommitDisposition.NotCommitted, scope.GetCommitDisposition(context));
    }

    [Fact]
    public async Task should_not_issue_validate_only_append_when_command_fails()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<CorrelationId>()).Returns(unit);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IUnitOfWorkManager)).Returns(manager);
        var scope = new TransactionalCommandScope();
        var correlation = CorrelationId.New();
        var context = new CommandContext(correlation, typeof(Command), new Command(), [], new(), ServiceProvider: services);
        scope.Begin(context);
        unit.AddDecisionRead(DecisionFixtures.Protected<Model>("source"));
        await scope.Complete(context, CommandResult.Error(correlation, "rejected"));
        Assert.True(unit.IsCompleted);
        await log.DidNotReceiveWithAnyArgs().AppendMany(default!);
    }

    [Fact]
    public void should_register_command_aware_resolution_from_direct_add_read_models()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Projections.Returns([typeof(for_ReadModelServiceCollectionExtensions.ProjectionForReadModel)]);
        artifacts.ModelBoundProjections.Returns([]);
        artifacts.Reducers.Returns([]);
        var services = new ServiceCollection().AddReadModels(artifacts);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDecisionReads) &&
            descriptor.Lifetime == ServiceLifetime.Scoped && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(DecisionRead<for_ReadModelServiceCollectionExtensions.ProjectionReadModel>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICommandDependencySafety) &&
            descriptor.ImplementationType == typeof(DecisionDependencySafety));
    }

    [Fact]
    public async Task should_keep_validation_only_reads_detached_and_refold_when_executing_with_the_same_provider()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var advisory = DecisionFixtures.Protected<Model>("source");
        var protectedRead = DecisionFixtures.Protected<Model>("source");
        inner.GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>()).Returns(advisory, protectedRead);
        var reader = new CommandDecisionReads(inner, store, Substitute.For<IReadModels>());
        var begin = typeof(CommandValidationExecution).GetMethod("Begin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(begin);
        using (var validation = Assert.IsType<IDisposable>(begin.Invoke(null, [typeof(Command)]), exactMatch: false))
        {
            Assert.Same(advisory, await reader.Get<Model>((ReadModelKey)"source"));
            Assert.Same(advisory, await reader.Get<Model>((ReadModelKey)"source"));
            Assert.False(unit.HasEnrolledDecisionReads);
        }
        CommandTransaction.Current = unit;
        CommandDecisionReads.Begin(typeof(Command));
        try
        {
            Assert.Same(protectedRead, await reader.Get<Model>((ReadModelKey)"source"));
            Assert.True(unit.HasEnrolledDecisionReads);
            await inner.Received(2).GetDetached<Model>((ReadModelKey)"source", Arg.Any<CancellationToken>());
        }
        finally
        {
            CommandDecisionReads.End();
            CommandTransaction.Current = null;
        }
    }

    [Fact]
    public async Task should_not_complete_outer_frame_when_nested_begin_never_reached_this_scope()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<CorrelationId>()).Returns(unit);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IUnitOfWorkManager)).Returns(manager);
        var scope = new TransactionalCommandScope();
        var outerId = CorrelationId.New();
        var outer = new CommandContext(outerId, typeof(Command), new Command(), [], new(), ServiceProvider: services);
        var nested = new CommandContext(CorrelationId.New(), typeof(NestedCommand), new NestedCommand(), [], new(), ServiceProvider: services);
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(AppendManyResult.Success(outerId, []));
        scope.Begin(outer);
        unit.AddDecisionRead(DecisionFixtures.Protected<Model>("source"));
        await scope.Complete(nested, CommandResult.Error(nested.CorrelationId, "nested Begin failed"));
        Assert.False(unit.IsCompleted);
        Assert.True(CommandTransaction.TryGetActive(out var active));
        Assert.Same(unit, active);
        await scope.Complete(outer, CommandResult.Success(outerId));
        Assert.True(unit.IsCompleted);
        Assert.False(CommandTransaction.TryGetActive(out _));
    }

    [Fact]
    public async Task should_restore_the_owning_scope_after_a_nested_command_completes()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(Arg.Any<CorrelationId>()).Returns(unit);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IUnitOfWorkManager)).Returns(manager);
        var scope = new TransactionalCommandScope();
        var outerId = CorrelationId.New();
        var outer = new CommandContext(outerId, typeof(Command), new Command(), [], new(), ServiceProvider: services);
        var nested = new CommandContext(CorrelationId.New(), typeof(NestedCommand), new NestedCommand(), [], new(), ServiceProvider: services);
        log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(AppendManyResult.Success(outerId, []));
        scope.Begin(outer);
        unit.AddDecisionRead(DecisionFixtures.Protected<Model>("source"));
        scope.Begin(nested);
        await scope.Complete(nested, CommandResult.Success(nested.CorrelationId));
        Assert.False(unit.IsCompleted);
        Assert.True(CommandTransaction.TryGetActive(out var active));
        Assert.Same(unit, active);
        await scope.Complete(outer, CommandResult.Success(outerId));
        Assert.True(unit.IsCompleted);
        Assert.False(CommandTransaction.TryGetActive(out _));
    }

    [Fact]
    public async Task should_use_legacy_reads_for_unprotected_commands_during_validation()
    {
        var (store, _, _) = DecisionFixtures.Transaction();
        var inner = Substitute.For<IDecisionReads>();
        var legacy = Substitute.For<IReadModels>();
        var model = new Model();
        legacy.GetInstanceById<Model>((ReadModelKey)"source").Returns(model);
        legacy.Release(model).Returns(model);
        var reader = new CommandDecisionReads(inner, store, legacy);
        var begin = typeof(CommandValidationExecution).GetMethod("Begin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(begin);
        using (Assert.IsType<IDisposable>(begin.Invoke(null, [typeof(UnprotectedCommand)]), exactMatch: false))
        {
            var read = await reader.Get<Model>((ReadModelKey)"source");
            Assert.False(read.IsProtected);
            Assert.Same(model, read.Instance);
        }

        await inner.DidNotReceiveWithAnyArgs().GetDetached<Model>((ReadModelKey)"source");
        await legacy.Received(1).GetInstanceById<Model>((ReadModelKey)"source");
    }

    public class Model;
    public class Command;
    public class NestedCommand;
    [Unprotected]
    public class UnprotectedCommand;
}
