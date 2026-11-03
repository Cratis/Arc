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
/// <param name="frameworkIssuedTokens">The models whose <c>DecisionRead&lt;T&gt;</c> factory the framework itself registered. Filled while the models are registered; a token whose factory the application registered first is left to that factory.</param>
internal sealed class ChronicleParameterDependencyResolver(IEnumerable<Type> readModelTypes, ISet<Type>? frameworkIssuedTokens = null) : IAsyncParameterDependencyResolver
{
    readonly HashSet<Type> _readModelTypes = readModelTypes.ToHashSet();

    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        dependency = null;
        var isReadModel = Measure(serviceProvider, parameter);
        if (isReadModel && PerStreamReadModelBinding.IsDeclared(parameter.ParameterType))
        {
            var commandContext = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
            dependency = PerStreamReadModelBinding.Resolve(parameter.ParameterType, commandContext, serviceProvider, parameter);
            return true;
        }
        if (!IsStoredLookup(parameter, isReadModel))
        {
            return false;
        }

        var context = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection, ProtectedReadModelKeys.Traversal(serviceProvider), out var keyText);
        var advisory = parameter.GetCustomAttribute<UnprotectedAttribute>() ?? parameter.ParameterType.GetCustomAttribute<UnprotectedAttribute>();
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null)
        {
            dependency = ProtectedReadModelKeys.RequireInstance(ResolveProtected(serviceProvider, context, parameter, key, keyText), parameter, keyText);
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
            dependency = ReadModelServiceCollectionExtensions.ResolveReadModel(parameter.ParameterType, context, serviceProvider.GetRequiredService<IReadModels>(), key);
        }

        return true;
    }

    /// <inheritdoc/>
    public async ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        var isReadModel = Measure(serviceProvider, parameter);
        if (IsFrameworkIssuedToken(parameter.ParameterType, out var tokenModel))
        {
            return ParameterDependencyResolution.Resolved(await ReadModelDispatch.For(tokenModel).ResolveTokenAsync(serviceProvider));
        }
        if (isReadModel && PerStreamReadModelBinding.IsDeclared(parameter.ParameterType))
        {
            var commandContext = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
            return ParameterDependencyResolution.Resolved(await PerStreamReadModelBinding.ResolveAsync(parameter.ParameterType, commandContext, serviceProvider, parameter));
        }
        if (!IsStoredLookup(parameter, isReadModel))
        {
            return ParameterDependencyResolution.NotResolved;
        }

        var context = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection, ProtectedReadModelKeys.Traversal(serviceProvider), out var keyText);
        var advisory = parameter.GetCustomAttribute<UnprotectedAttribute>() ?? parameter.ParameterType.GetCustomAttribute<UnprotectedAttribute>();
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null)
        {
            return ParameterDependencyResolution.Resolved(ProtectedReadModelKeys.RequireInstance(await ResolveProtectedAsync(serviceProvider, context, parameter, key, keyText), parameter, keyText));
        }

        using var allowance = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && TransactionalCommandScope.CurrentProtectedUnit is { } unit
            ? unit.AllowReads(ReadAllowance.Advisory, advisory?.Reason ?? "Declared advisory")
            : null;
        if (advisory is not null && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ReadProtectionTelemetry.Record(context.Type, "advisory_read", parameter.ParameterType, advisory.Reason ?? "Declared advisory");
        }
        return ParameterDependencyResolution.Resolved(await ReadModelServiceCollectionExtensions.ResolveReadModelAsync(parameter.ParameterType, context, serviceProvider.GetRequiredService<IReadModels>(), key));
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
    internal static T? ResolveModel<T>(IServiceProvider services, CommandContext context, ReadModelKey key)
        where T : class =>
        AdmittedReader<T>(services).GetInstance<T>(key, context.CancellationToken).GetAwaiter().GetResult();

    /// <summary>
    /// Resolves the model with its concrete type without blocking, for typed dispatch.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="services">The command services.</param>
    /// <param name="context">The command invocation.</param>
    /// <param name="key">The resolved read model key.</param>
    /// <returns>The model, or null when it does not exist.</returns>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">The reader is not command aware.</exception>
    /// <exception cref="DecisionReadRefused">The read cannot be protected.</exception>
    internal static async ValueTask<object?> ResolveModelAsync<T>(IServiceProvider services, CommandContext context, ReadModelKey key)
        where T : class =>
        await AdmittedReader<T>(services).GetInstance<T>(key, context.CancellationToken);

    static CommandDecisionReads AdmittedReader<T>(IServiceProvider services)
        where T : class
    {
        var reader = services.GetRequiredService<IDecisionReads>() as CommandDecisionReads ?? throw new ProtectedCommandRequiresCommandAwareReader();
        var description = reader.Describe<T>();
        if (!description.Admission.IsAdmitted || description.UsesPinnedGenerationDelivery)
        {
            var reason = description.UsesPinnedGenerationDelivery ? DecisionReadRefusalReason.GenerationDeliveryNotSupported : description.Admission.Reason!.Value;
            throw new DecisionReadRefused(reason, typeof(T));
        }

        return reader;
    }

    static async ValueTask<object?> ResolveProtectedAsync(IServiceProvider services, CommandContext context, ParameterInfo parameter, ReadModelKey key, string keyText)
    {
        try
        {
            return await ReadModelDispatch.For(parameter.ParameterType).ResolveProtectedAsync(services, context, key);
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

    static object? ResolveProtected(IServiceProvider services, CommandContext context, ParameterInfo parameter, ReadModelKey key, string keyText)
    {
        try
        {
            return ReadModelDispatch.For(parameter.ParameterType).ResolveProtected(services, context, key);
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

    static bool IsStoredLookup(ParameterInfo parameter, bool isReadModel) =>
        isReadModel &&
        (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On ||
         Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().Any());

    bool Measure(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        var isReadModel = _readModelTypes.Contains(parameter.ParameterType);
        ReadProtectionMeasurement.Current?.Parameter(parameter, isReadModel);
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && IsChronicleCollection(parameter.ParameterType))
        {
            var model = parameter.ParameterType.GetGenericArguments()[0];
            throw new ReadCannotBeProtected(serviceProvider.GetRequiredService<ICommandContextAccessor>().Current.Type, parameter.Name!, model, "through IMongoCollection", $"inject {model.Name}");
        }

        return isReadModel;
    }

    bool IsFrameworkIssuedToken(Type type, out Type model)
    {
        model = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>) ? type.GetGenericArguments()[0] : typeof(object);
        return frameworkIssuedTokens?.Contains(model) == true;
    }

    bool IsChronicleCollection(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" &&
        _readModelTypes.Contains(type.GetGenericArguments()[0]);
}
