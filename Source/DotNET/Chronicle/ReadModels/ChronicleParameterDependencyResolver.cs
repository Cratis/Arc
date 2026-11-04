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
/// <param name="isFrameworkToken">Identifies the effective framework token factory for the executing root provider. Application-owned registrations retain ordinary DI resolution.</param>
internal sealed class ChronicleParameterDependencyResolver(IEnumerable<Type> readModelTypes, Func<IServiceProvider, Type, bool>? isFrameworkToken = null) : IAsyncParameterDependencyResolver
{
    readonly HashSet<Type> _readModelTypes = readModelTypes.ToHashSet();

    enum BindingKind
    {
        Stored = 0,
        Protected = 1,
        Stream = 2,
        Token = 3
    }

    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        dependency = null;
        var binding = Plan(serviceProvider, parameter);
        if (binding is null || binding.Kind == BindingKind.Token)
        {
            return false;
        }

        using var allowance = binding.AllowReads();
        dependency = binding.Kind switch
        {
            BindingKind.Stream => PerStreamReadModelBinding.Resolve(binding.Model, binding.Context, serviceProvider, parameter),
            BindingKind.Protected => ProtectedReadModelKeys.RequireInstance(ResolveProtected(serviceProvider, binding), parameter, binding.KeyText),
            _ => ReadModelServiceCollectionExtensions.ResolveReadModel(binding.Model, binding.Context, serviceProvider.GetRequiredService<IReadModels>(), binding.Key)
        };
        return true;
    }

    /// <inheritdoc/>
    public async ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter)
    {
        var binding = Plan(serviceProvider, parameter);
        if (binding is null)
        {
            return ParameterDependencyResolution.NotResolved;
        }

        binding.Context.CancellationToken.ThrowIfCancellationRequested();
        using var allowance = binding.AllowReads();
        var dependency = binding.Kind switch
        {
            BindingKind.Token => await ReadModelDispatch.For(binding.Model).ResolveTokenAsync(serviceProvider),
            BindingKind.Stream => await PerStreamReadModelBinding.ResolveAsync(binding.Model, binding.Context, serviceProvider, parameter),
            BindingKind.Protected => ProtectedReadModelKeys.RequireInstance(await ResolveProtectedAsync(serviceProvider, binding), parameter, binding.KeyText),
            _ => await ReadModelServiceCollectionExtensions.ResolveReadModelAsync(binding.Model, binding.Context, serviceProvider.GetRequiredService<IReadModels>(), binding.Key)
        };
        return ParameterDependencyResolution.Resolved(dependency);
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

    static bool RequiresParameterLookup(ParameterInfo parameter, bool isReadModel) => isReadModel &&
        (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On ||
         CommandAttributePolicy.HasExplicitLookup(parameter) ||
         Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().Any());

    static object? ResolveProtected(IServiceProvider services, ParameterBinding binding)
    {
        try
        {
            return ReadModelDispatch.For(binding.Model).ResolveProtected(services, binding.Context, binding.Key);
        }
        catch (Exception failure) when (IsRefusal(failure))
        {
            throw binding.Refusal(failure);
        }
    }

    static async ValueTask<object?> ResolveProtectedAsync(IServiceProvider services, ParameterBinding binding)
    {
        try
        {
            return await ReadModelDispatch.For(binding.Model).ResolveProtectedAsync(services, binding.Context, binding.Key);
        }
        catch (Exception failure) when (IsRefusal(failure))
        {
            throw binding.Refusal(failure);
        }
    }

    static bool IsRefusal(Exception failure) => failure is ProtectedReadModelDoesNotExist or DecisionReadRefused or { InnerException: DecisionReadRefused };

    static void ValidateTokenLookup(ParameterInfo parameter)
    {
        if (LookUpByKeyProviderDeclarations.For(parameter).FirstOrDefault(LookUpByKeyProviderDeclarations.IsProvider) is { } provider)
        {
            throw new InvalidLookUpBy(parameter.Member.DeclaringType!, provider.GetType().GenericTypeArguments[0], "is only supported for Chronicle read models");
        }
    }

    static ParameterBinding LookupPlan(IServiceProvider services, ParameterInfo parameter, CommandContext context)
    {
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection, ProtectedReadModelKeys.Traversal(services), out var keyText);
        var advisory = parameter.GetCustomAttribute<UnprotectedAttribute>() ?? parameter.ParameterType.GetCustomAttribute<UnprotectedAttribute>();
        if (advisory is not null && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ReadProtectionTelemetry.Record(context.Type, "advisory_read", parameter.ParameterType, advisory.Reason ?? "Declared advisory");
        }
        return new(parameter, context, parameter.ParameterType)
        {
            Kind = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null ? BindingKind.Protected : BindingKind.Stored,
            Key = key,
            KeyText = keyText,
            Advisory = advisory
        };
    }

    ParameterBinding? Plan(IServiceProvider services, ParameterInfo parameter)
    {
        var isReadModel = Measure(services, parameter);
        if (isFrameworkToken?.Invoke(services, parameter.ParameterType) == true)
        {
            ValidateTokenLookup(parameter);
            return new(parameter, services.GetRequiredService<ICommandContextAccessor>().Current, parameter.ParameterType.GetGenericArguments()[0]) { Kind = BindingKind.Token };
        }
        if (isReadModel && PerStreamReadModelBinding.IsDeclared(parameter.ParameterType))
        {
            return new(parameter, services.GetRequiredService<ICommandContextAccessor>().Current, parameter.ParameterType) { Kind = BindingKind.Stream };
        }
        return RequiresParameterLookup(parameter, isReadModel)
            ? LookupPlan(services, parameter, services.GetRequiredService<ICommandContextAccessor>().Current)
            : null;
    }

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

    bool IsChronicleCollection(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition().FullName == "MongoDB.Driver.IMongoCollection`1" &&
        _readModelTypes.Contains(type.GetGenericArguments()[0]);

    sealed record ParameterBinding(ParameterInfo Parameter, CommandContext Context, Type Model)
    {
        public BindingKind Kind { get; init; }
        public ReadModelKey Key { get; init; } = null!;
        public string KeyText { get; init; } = string.Empty;
        public UnprotectedAttribute? Advisory { get; init; }

        public IDisposable? AllowReads() => Kind == BindingKind.Stored && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && TransactionalCommandScope.CurrentProtectedUnit is { } unit
            ? unit.AllowReads(ReadAllowance.Advisory, Advisory?.Reason ?? "Declared advisory")
            : null;

        public Exception Refusal(Exception failure)
        {
            if (failure is ProtectedReadModelDoesNotExist)
            {
                return new ProtectedReadModelDoesNotExist(Model, KeyText);
            }
            var refused = (failure as DecisionReadRefused ?? failure.InnerException as DecisionReadRefused)!;
            return ReadProtectionRefusals.For(Context.Type, Parameter, refused.Reason);
        }
    }
}
