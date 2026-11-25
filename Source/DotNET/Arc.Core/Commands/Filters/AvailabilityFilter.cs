// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that refuses a command in a part that is not available, before authorization.
/// </summary>
/// <remarks>
/// The decision comes from the registered <see cref="IAvailabilityGate"/>. Without one the filter does nothing. An
/// unavailable or unknown decision is refused with <see cref="AvailabilityReason.NotAvailable"/>; a failure to decide
/// is a retryable error, never a denial.
/// </remarks>
public class AvailabilityFilter : IAvailabilityCommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (OperationSnapshot.Current is { } snapshot)
        {
            InvocationContextValues.Attach(snapshot, context.ProvidedValues);
        }

        var gate = context.ServiceProvider?.GetService<IAvailabilityGate>();
        if (gate is null || context.ServiceProvider is null || !gate.IsGated(context.Type))
        {
            return CommandResult.Success(context.CorrelationId);
        }

        return await Decide(context, gate, context.ServiceProvider);
    }

    static async Task<CommandResult> Decide(CommandContext context, IAvailabilityGate gate, IServiceProvider services)
    {
        try
        {
            var decision = gate is IAvailabilityExecutionGate executionGate
                ? await executionGate.Decide(context.AvailabilityInvocation, services, context.CancellationToken)
                : await gate.Decide(context.Type, services, context.CancellationToken);
            return decision.IsAvailable
                ? CommandResult.Success(context.CorrelationId)
                : CommandResult.Unauthorized(context.CorrelationId, decision.Reason);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            services.GetService<ILogger<AvailabilityFilter>>()?.AvailabilityNotDecided(context.Type.FullName ?? context.Type.Name, ex);
            return CommandResult.FromException(context.CorrelationId, new AvailabilityNotDecidedException(ex));
        }
    }
}
