// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Represents an <see cref="ICanResolveReadModelForCommand"/> that resolves read models backed by Chronicle through
/// <see cref="IReadModels"/>, keyed by the command's resolved key (the event source id).
/// </summary>
/// <param name="readModelTypes">The read model types Chronicle can resolve by key.</param>
public class ChronicleReadModelForCommandResolver(IEnumerable<Type> readModelTypes) : ICanResolveReadModelForCommand
{
    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelTypes { get; } = readModelTypes;

    /// <inheritdoc/>
    /// <remarks>
    /// A Chronicle projection, model-bound projection, or reducer in the application targets each of these read models,
    /// which is what makes Chronicle own them — and what makes Chronicle the provider that has to resolve them, since it
    /// is the only one that releases their compliance-protected values.
    /// <para>
    /// What follows from declaring: an instance resolved this way is deserialized from a JSON payload with
    /// <c>System.Text.Json</c>, so it never meets the MongoDB driver. No convention pack, class-map customization,
    /// element rename or custom <c>IBsonSerializer</c> applies to it, even where the same customization is visibly at
    /// work on the query side.
    /// </para>
    /// </remarks>
    public ReadModelForCommandOwnership Ownership => ReadModelForCommandOwnership.Declared;

    /// <inheritdoc/>
    /// <remarks>
    /// Under read protection <c>On</c>, Arc binds the read models of <c>Handle()</c> and of validator constructors itself
    /// and guards them. A read model that reaches this resolver instead was injected somewhere else in the command, for
    /// example into a command filter or a service, where no guard is enrolled for it. Such a read is refused unless the
    /// model type is marked <see cref="UnprotectedAttribute"/>, in which case it is an advisory read that guards nothing.
    /// </remarks>
    /// <exception cref="ReadModelInjectedOutsideDecision">The read model was injected outside the decision under read protection <c>On</c>.</exception>
    public Task<object?> Resolve(Type readModelType, CommandContext commandContext)
    {
        var readModels = commandContext.ServiceProvider!.GetRequiredService<IReadModels>();
        if (CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On || PerStreamReadModelBinding.IsDeclared(readModelType))
        {
            return Task.FromResult(ReadModelServiceCollectionExtensions.ResolveReadModel(readModelType, commandContext, readModels));
        }

        var advisory = readModelType.GetCustomAttribute<UnprotectedAttribute>() ??
            throw new ReadModelInjectedOutsideDecision(commandContext.Type, readModelType);
        var reason = advisory.Reason ?? "Declared advisory";
        ReadProtectionTelemetry.Record(commandContext.Type, "advisory_read", readModelType, reason);
        using var allowance = TransactionalCommandScope.CurrentProtectedUnit?.AllowReads(ReadAllowance.Advisory, reason);
        return Task.FromResult(ReadModelServiceCollectionExtensions.ResolveReadModel(readModelType, commandContext, readModels));
    }
}
