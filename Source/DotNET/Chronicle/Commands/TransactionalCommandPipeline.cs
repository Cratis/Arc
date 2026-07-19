// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an <see cref="ICommandPipeline"/> that makes every command a transactional scope: it begins a
/// <see cref="IUnitOfWork"/> around the command, commits it only when the command succeeds — surfacing any constraint
/// or concurrency violations from the atomic commit into the <see cref="CommandResult"/> — and rolls it back for any
/// non-successful command. The invariant is that a command whose result is not successful appends no events at all.
/// </summary>
/// <param name="inner">The underlying <see cref="CommandPipeline"/> that runs filters, the handler and response handlers.</param>
/// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/> used to create a command scope for the overloads that do not receive one.</param>
/// <remarks>
/// The <see cref="IUnitOfWorkManager"/> is resolved from the command's own service provider — never captured — so the
/// unit of work is created over the tenant-correct event store for the command being executed.
/// </remarks>
public class TransactionalCommandPipeline(CommandPipeline inner, IServiceScopeFactory scopeFactory) : ICommandPipelineWithCancellation
{
    /// <inheritdoc/>
    public Task<CommandResult> Execute(object command, ValidationResultSeverity? allowedSeverity = default) =>
        TransactionallyInNewScope(serviceProvider => inner.Execute(command, serviceProvider, allowedSeverity));

    /// <inheritdoc/>
    public Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Transactionally(serviceProvider, provider => inner.Execute(command, provider, allowedSeverity));

    /// <inheritdoc/>
    public Task<CommandResult> Execute(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        TransactionallyInNewScope(serviceProvider => inner.Execute(command, serviceProvider, allowedSeverity, cancellationToken));

    /// <inheritdoc/>
    public Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        Transactionally(serviceProvider, provider => inner.Execute(command, provider, allowedSeverity, cancellationToken));

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> Execute<TResult>(object command, ValidationResultSeverity? allowedSeverity = default) =>
        TransactionallyInNewScope(serviceProvider => inner.Execute<TResult>(command, serviceProvider, allowedSeverity));

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> Execute<TResult>(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        Transactionally(serviceProvider, provider => inner.Execute<TResult>(command, provider, allowedSeverity));

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> Execute<TResult>(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        TransactionallyInNewScope(serviceProvider => inner.Execute<TResult>(command, serviceProvider, allowedSeverity, cancellationToken));

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> Execute<TResult>(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        Transactionally(serviceProvider, provider => inner.Execute<TResult>(command, provider, allowedSeverity, cancellationToken));

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, ValidationResultSeverity? allowedSeverity = default) =>
        inner.Validate(command, allowedSeverity);

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default) =>
        inner.Validate(command, serviceProvider, allowedSeverity);

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        inner.Validate(command, allowedSeverity, cancellationToken);

    /// <inheritdoc/>
    public Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity, CancellationToken cancellationToken) =>
        inner.Validate(command, serviceProvider, allowedSeverity, cancellationToken);

    async Task<TResult> TransactionallyInNewScope<TResult>(Func<IServiceProvider, Task<TResult>> execute)
        where TResult : CommandResult
    {
        using var scope = scopeFactory.CreateScope();
        return await Transactionally(scope.ServiceProvider, execute);
    }

    async Task<TResult> Transactionally<TResult>(IServiceProvider serviceProvider, Func<IServiceProvider, Task<TResult>> execute)
        where TResult : CommandResult
    {
        var unitOfWorkManager = serviceProvider.GetRequiredService<IUnitOfWorkManager>();

        // A nested command reuses the outermost command's unit of work — only the outermost begins and commits.
        if (unitOfWorkManager.HasCurrent)
        {
            return await execute(serviceProvider);
        }

        var unitOfWork = unitOfWorkManager.Begin(CorrelationId.New());
        var result = await execute(serviceProvider);

        if (result.IsSuccess)
        {
            await unitOfWork.Commit();
            var commitResult = AggregateRootCommitResult.CreateFrom(unitOfWork, []);
            if (!commitResult.IsSuccess)
            {
                result.MergeWith(commitResult.ToCommandResult(result.CorrelationId));
            }
        }
        else if (!unitOfWork.IsCompleted)
        {
            await unitOfWork.Rollback();
        }

        return result;
    }
}
