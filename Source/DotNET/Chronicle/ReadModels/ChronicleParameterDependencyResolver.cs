// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
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
        dependency = null;
        var isReadModel = _readModelTypes.Contains(parameter.ParameterType);
        ReadProtectionMeasurement.Current?.Parameter(parameter, isReadModel);
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && IsChronicleCollection(parameter.ParameterType))
        {
            var model = parameter.ParameterType.GetGenericArguments()[0];
            throw new ReadCannotBeProtected(serviceProvider.GetRequiredService<ICommandContextAccessor>().Current.Type, parameter.Name!, model, "through IMongoCollection", $"inject {model.Name}");
        }
        if (isReadModel && PerStreamReadModelBinding.IsDeclared(parameter.ParameterType))
        {
            var commandContext = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
            dependency = PerStreamReadModelBinding.Resolve(parameter.ParameterType, commandContext, serviceProvider, parameter);
            return true;
        }
        if (!isReadModel ||
            (CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On &&
             !Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().Any()))
        {
            return false;
        }

        var context = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection, out var keyText);
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

    static object? ResolveProtected(IServiceProvider services, CommandContext context, ParameterInfo parameter, ReadModelKey key, string keyText)
    {
        try
        {
            return typeof(ChronicleParameterDependencyResolver).GetMethod(nameof(ResolveModel), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(parameter.ParameterType).Invoke(null, [services, context, key]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            var failure = exception.InnerException;
            if (failure is ProtectedReadModelDoesNotExist)
            {
                throw new ProtectedReadModelDoesNotExist(parameter.ParameterType, keyText);
            }
            if ((failure as DecisionReadRefused ?? failure.InnerException as DecisionReadRefused) is { } refused)
            {
                throw ReadProtectionRefusals.For(context.Type, parameter, refused.Reason);
            }
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    static T? ResolveModel<T>(IServiceProvider services, CommandContext context, ReadModelKey key)
        where T : class
    {
        var reader = services.GetRequiredService<IDecisionReads>() as CommandDecisionReads ?? throw new ProtectedCommandRequiresCommandAwareReader();
        var description = reader.Describe<T>();
        if (!description.Admission.IsAdmitted || description.UsesPinnedGenerationDelivery)
        {
            var reason = description.UsesPinnedGenerationDelivery ? DecisionReadRefusalReason.GenerationDeliveryNotSupported : description.Admission.Reason!.Value;
            throw new DecisionReadRefused(reason, typeof(T));
        }

        return reader.GetInstance<T>(key, context.CancellationToken).GetAwaiter().GetResult();
    }

    bool IsChronicleCollection(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" &&
        _readModelTypes.Contains(type.GetGenericArguments()[0]);
}
