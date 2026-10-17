// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Occurrences;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Represents an implementation of <see cref="IOccurrenceCommandExecutor"/>.
/// </summary>
/// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/> used to open the tenant's scope.</param>
/// <param name="systemExecution">The <see cref="ISystemExecution"/> establishing the system identity.</param>
/// <param name="tenantScope">The <see cref="ITenantScope"/> selecting the tenant the occurrence is delivered in.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[Singleton]
public class OccurrenceCommandExecutor(
    IServiceScopeFactory serviceScopeFactory,
    ISystemExecution systemExecution,
    ITenantScope tenantScope,
    ILogger<OccurrenceCommandExecutor> logger) : IOccurrenceCommandExecutor
{
    /// <inheritdoc/>
    public Task<OccurrenceCommandResult> Execute(object command, OccurrenceContext occurrence, EffectName effectName, CancellationToken cancellationToken = default) =>
        Execute(command, occurrence, effectName, [], cancellationToken);

    /// <inheritdoc/>
    public async Task<OccurrenceCommandResult> Execute(object command, OccurrenceContext occurrence, EffectName effectName, IEnumerable<string> systemRoles, CancellationToken cancellationToken = default)
    {
        if (CommandTransaction.TryGetActive(out _) || OccurrenceExecution.Current is not null)
        {
            throw new OccurrenceCommandCannotRunInsideCommand("An occurrence command must be executed outside any running command: one effect receipt covers one unit of work that the executor begins itself.");
        }

        var key = occurrence.EffectKey(effectName);
        var commandName = command.GetType().Name;
        var namespaceName = occurrence.Namespace ?? EventStoreNamespaceName.Default;

        using var tenant = tenantScope.Begin(namespaceName == EventStoreNamespaceName.Default ? TenantId.Default : new TenantId(namespaceName.Value));
        using var scope = serviceScopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var eventStore = services.GetRequiredService<IEventStore>();
        if (eventStore.Namespace != namespaceName)
        {
            throw new OccurrenceCommandNamespaceMismatch(namespaceName.Value, eventStore.Namespace.Value);
        }

        // Fails closed with EffectReceiptsNotSupported when the capability is absent or unknown. Only an optimization:
        // the commit still returns the winning receipt when another writer got there first.
        var stored = await eventStore.Occurrences.GetEffectReceipt(key);
        if (stored is not null)
        {
            logger.ReturningStoredReceipt(key.Value, commandName);
            return Stored(stored);
        }

        using var system = systemExecution.AsSystem([.. systemRoles]);
        var correlationIdAccessor = services.GetRequiredService<ICorrelationIdAccessor>();
        var previousCorrelationId = correlationIdAccessor.Current;
        Action<CorrelationId> modifyCorrelationId = correlationIdAccessor is ICorrelationIdModifier modifier
            ? modifier.Modify
            : CorrelationIdAccessor.SetCurrent;
        try
        {
            modifyCorrelationId(CorrelationId.New());
            using var causation = services.GetService<ICausationManager>()?.BeginScope(OccurrenceCausation.Type, OccurrenceCausation.PropertiesFor(occurrence, key));

            var execution = new OccurrenceExecution(key, commandName);
            CommandResult result;
            using (OccurrenceExecution.Begin(execution))
            {
                var pipeline = services.GetRequiredService<ICommandPipeline>();
                result = pipeline is ICommandPipelineWithCancellation cancellable
                    ? await cancellable.Execute(command, services, null, cancellationToken)
                    : await pipeline.Execute(command, services);
            }

            if (execution.AppendedImmediately)
            {
                throw new OccurrenceCommandAppendedImmediately(key.Value);
            }

            execution.ThrowIfRefused();
            if (execution.Receipt is { } committed)
            {
                return Settled(committed, result);
            }

            return result.IsSuccess
                ? await Succeeded(eventStore, key, commandName, execution, result)
                : await Failed(eventStore, key, commandName, result);
        }
        finally
        {
            modifyCorrelationId(previousCorrelationId);
        }
    }

    static OccurrenceCommandResult Stored(EffectReceipt receipt) =>
        new(OccurrenceCommandDisposition.AlreadyRecorded, receipt.Outcome, receipt.Response, null);

    static OccurrenceCommandResult Settled(EffectReceiptResult receiptResult, CommandResult result) =>
        receiptResult.Status == EffectReceiptStatus.AlreadyRecorded
            ? Stored(receiptResult.Receipt)
            : new(OccurrenceCommandDisposition.Recorded, receiptResult.Receipt.Outcome, receiptResult.Receipt.Response, result);

    async Task<OccurrenceCommandResult> Succeeded(IEventStore eventStore, EffectKey key, string commandName, OccurrenceExecution execution, CommandResult result)
    {
        if (execution.HadEvents || execution.Enrolled)
        {
            // Events were committed (or a protected decision was validated) but the commit reported no receipt.
            throw new OccurrenceCommandReceiptNotCommitted(key.Value);
        }

        var response = OccurrenceReceiptResponses.NoEvents(commandName);
        return Settled(await eventStore.Occurrences.RecordEffectReceipt(key, new NoEvents(), response), result);
    }

    async Task<OccurrenceCommandResult> Failed(IEventStore eventStore, EffectKey key, string commandName, CommandResult result)
    {
        // Only a definite business refusal is terminal. Authorization failures, exceptions, concurrency conflicts and
        // anything unclassified can succeed on a later attempt, so they leave no receipt.
        if (result.GetRefusal() is not { Kind: not CommandRefusalKind.Authorization } refusal)
        {
            logger.AttemptFailed(key.Value, commandName);
            return new(OccurrenceCommandDisposition.Failed, null, null, result);
        }

        var reasons = OccurrenceReceiptResponses.ReasonsFor(result, refusal).ToArray();
        var recorded = await eventStore.Occurrences.RecordEffectReceipt(key, new Refused(reasons), OccurrenceReceiptResponses.Refused(commandName, reasons));
        logger.RefusalRecorded(key.Value, commandName);

        return Settled(recorded, result);
    }
}
