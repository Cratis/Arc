// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Resolves Chronicle-backed parameters independently of type-based scoped registrations.
/// </summary>
/// <param name="readModelTypes">The models backed by Chronicle artifacts.</param>
internal sealed class ChronicleParameterDependencyResolver(IEnumerable<Type> readModelTypes) : ICanResolveParameterDependency
{
    readonly HashSet<Type> _readModelTypes = readModelTypes.ToHashSet();

    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        var isReadModel = _readModelTypes.Contains(parameter.ParameterType);
        if (!OwnsSynchronousParameter(parameter, isReadModel))
        {
            ReadProtectionMeasurement.Current?.Parameter(parameter, isReadModel);
            dependency = null;
            return false;
        }
        var result = TryResolveAsync(serviceProvider, parameter).GetAwaiter().GetResult();
        dependency = result.Dependency;
        return result.Resolved;
    }

    /// <inheritdoc/>
    public async ValueTask<(bool Resolved, object? Dependency)> TryResolveAsync(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(DecisionRead<>))
        {
            var commandContext = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
            return (true, await ReadModelServiceCollectionExtensions.ResolveDecisionReadAsync(parameter.ParameterType, commandContext, serviceProvider));
        }

        var isReadModel = _readModelTypes.Contains(parameter.ParameterType);
        ReadProtectionMeasurement.Current?.Parameter(parameter, isReadModel);
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && IsChronicleCollection(parameter.ParameterType))
        {
            var model = parameter.ParameterType.GetGenericArguments()[0];
            throw new ReadCannotBeProtected(serviceProvider.GetRequiredService<ICommandContextAccessor>().Current.Type, parameter.Name!, model, "through IMongoCollection", $"inject {model.Name}");
        }
        if (!isReadModel)
        {
            return (false, null);
        }

        var context = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
        return (true, await ResolveReadModelAsync(serviceProvider, context, parameter));
    }

    bool OwnsSynchronousParameter(ParameterInfo parameter, bool isReadModel)
    {
        if (!isReadModel) return IsChronicleCollection(parameter.ParameterType);
        if (PerStreamReadModelBinding.IsDeclared(parameter.ParameterType)) return true;
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On) return true;
        return Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().Any();
    }

    static async Task<object?> ResolveReadModelAsync(IServiceProvider serviceProvider, CommandContext context, ParameterInfo parameter)
    {
        if (PerStreamReadModelBinding.IsDeclared(parameter.ParameterType))
        {
            return await PerStreamReadModelBinding.ResolveAsync(parameter.ParameterType, context, serviceProvider, parameter);
        }

        object? dependency;
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection, ProtectedReadModelKeys.Traversal(serviceProvider), out var keyText);
        var advisory = parameter.GetCustomAttribute<UnprotectedAttribute>() ?? parameter.ParameterType.GetCustomAttribute<UnprotectedAttribute>();
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null)
        {
            dependency = ProtectedReadModelKeys.RequireInstance(await ResolveProtected(serviceProvider, context, parameter, key, keyText), parameter, keyText);
        }
        else
        {
            using var allowance = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && TransactionalCommandScope.CurrentProtectedUnit is { } unit
                ? unit.AllowReads(ReadAllowance.Advisory, advisory?.Reason ?? "Declared advisory")
                : null;
            if (advisory is not null && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
            {
                ReadProtectionTelemetry.Record(context.Type, "advisory_read", parameter.ParameterType, advisory.Reason ?? "Declared advisory");
            }
            dependency = await ReadModelServiceCollectionExtensions.ResolveReadModelAsync(parameter.ParameterType, context, serviceProvider.GetRequiredService<IReadModels>(), key);
        }

        return dependency;
    }

    /// <summary>
    /// Resolves the model with its concrete type, for typed dispatch.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="services">The command services.</param>
    /// <param name="context">The command invocation.</param>
    /// <param name="key">The resolved read model key.</param>
    /// <returns>The model, or null when it does not exist.</returns>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">The reader is not command aware.</exception>
    /// <exception cref="DecisionReadRefused">The read cannot be protected.</exception>
    internal static async Task<T?> ResolveModel<T>(IServiceProvider services, CommandContext context, ReadModelKey key)
        where T : class
    {
        var reader = services.GetRequiredService<IDecisionReads>() as CommandDecisionReads ?? throw new ProtectedCommandRequiresCommandAwareReader();
        var description = reader.Describe<T>();
        if (!description.Admission.IsAdmitted || description.UsesPinnedGenerationDelivery)
        {
            var reason = description.UsesPinnedGenerationDelivery ? DecisionReadRefusalReason.GenerationDeliveryNotSupported : description.Admission.Reason!.Value;
            throw new DecisionReadRefused(reason, typeof(T));
        }

        return await reader.GetInstance<T>(key, context.CancellationToken);
    }

    static async Task<object?> ResolveProtected(IServiceProvider services, CommandContext context, ParameterInfo parameter, ReadModelKey key, string keyText)
    {
        try
        {
            return await ReadModelDispatch.For(parameter.ParameterType).ResolveProtected(services, context, key);
        }
        catch (ProtectedReadModelDoesNotExist)
        {
            throw new ProtectedReadModelDoesNotExist(parameter.ParameterType, keyText);
        }
        catch (Exception failure) when ((failure as DecisionReadRefused ?? failure.InnerException as DecisionReadRefused) is not null)
        {
            var refused = (failure as DecisionReadRefused ?? failure.InnerException as DecisionReadRefused)!;
            throw ReadProtectionRefusals.For(context.Type, parameter, refused.Reason);
        }
    }

    bool IsChronicleCollection(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" &&
        _readModelTypes.Contains(type.GetGenericArguments()[0]);
}
