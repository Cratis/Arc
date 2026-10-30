// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for read models.
/// </summary>
public static class ReadModelServiceCollectionExtensions
{
    static readonly ConditionalWeakTable<IClientArtifactsProvider, ReadModelDiscovery> _discoveryByArtifacts = new();
    static readonly MethodInfo _releaseMethod = typeof(IReadModels)
        .GetMethods()
        .Single(_ =>
        {
            var parameters = _.GetParameters();
            return _.Name == nameof(IReadModels.Release) &&
                   _.IsGenericMethodDefinition &&
                   parameters.Length == 1 &&
                   parameters[0].ParameterType.IsGenericParameter;
        });

    /// <summary>
    /// Adds read model auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for client artifacts.</param>
    /// <returns>The service collection for continuation.</returns>
    /// <remarks>
    /// A read model is injectable into command-scoped code (a <c>CommandValidator&lt;&gt;</c>, <c>Provide()</c>, or
    /// <c>Handle()</c>) because it is resolvable by key (the resolved event source id) through <see cref="IReadModels"/>.
    /// What makes a read model resolvable that way is a Chronicle backing artifact, so this registers every read model
    /// that has a projection, standalone model-bound projection, classic reducer, or self reduction — independent of whether it also carries the Arc-level
    /// <c>[ReadModel]</c> attribute. It deliberately does not register read models by <c>[ReadModel]</c> alone: that
    /// attribute is an Arc concept that does not imply Chronicle key resolution, and a read model backed by another
    /// provider (for example Entity Framework Core) is registered by that provider, not here.
    /// </remarks>
    public static IServiceCollection AddReadModels(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IInterceptReadModel<>), typeof(ReadModelInterceptor<>)));

        var readModelTypes = DiscoverReadModelTypes(clientArtifactsProvider);
        ProtectedDecisionRegistrationOrigins.For(services).RecognizeReaders(typeof(IDecisionReads), readModelTypes);

        // Contribute the Chronicle-backed read model types to the provider-neutral command-scope resolution. This
        // registers a scoped, by-key resolver for each type and adds them to the additive set that lets a missing
        // non-nullable read model be surfaced as a validation failure (HTTP 400), coexisting with any other provider.
        services.AddReadModelsForCommand(new ChronicleReadModelForCommandResolver(readModelTypes));
        services.AddSingleton(new ChronicleParameterDependencyResolver(readModelTypes));
        services.TryAddSingleton<IInstancesOf<ICanResolveParameterDependency>>(sp =>
            new KnownInstancesOf<ICanResolveParameterDependency>([sp.GetRequiredService<ChronicleParameterDependencyResolver>()]));

        // This public entry point also registers DecisionRead<T>. Never leave it bound to Chronicle's raw reader.
        services.AddCommandAwareDecisionReads();

        foreach (var readModelType in readModelTypes)
        {
            // One closed registration per read model; it registers the typed dispatch and the DecisionRead<T> factory.
            RegisterReadModel(readModelType, services, readModelTypes);
        }

        return services;
    }

    /// <summary>
    /// Adds the command invocation guard around Chronicle's decision reader.
    /// </summary>
    /// <param name="services">Services to configure for command-side decision reads.</param>
    /// <returns>These same services for further registrations.</returns>
    public static IServiceCollection AddCommandAwareDecisionReads(this IServiceCollection services)
    {
        services.AddOptions<ProtectedReadModelKeyOptions>();
        services.TryAddSingleton<ProtectedReadModelKeyTraversal>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ProtectedReadModelKeyStartup>());
        services.TryAddSingleton<IStartupDecisionReads, ChronicleStartupDecisionReads>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DecisionReadAdmissionStartup>());
        services.TryAddSingleton<ReadProtectionMeasureQueue>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ReadProtectionMeasureQueue>(sp => sp.GetRequiredService<ReadProtectionMeasureQueue>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICommandDependencySafety, DecisionDependencySafety>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICommandExceptionMapper, ReadProtectionCommandExceptionMapper>());
        services.TryAddSingleton<ICommandProtectedDecisionSupport, DecisionDependencySafety>();
        var descriptor = ServiceDescriptor.Scoped<IDecisionReads>(sp => new CommandDecisionReads(
            sp.GetRequiredService<IEventStore>().GetDecisionReads(),
            sp.GetRequiredService<IEventStore>(),
            sp.GetRequiredService<IReadModels>()));
        services.Add(descriptor);
        ProtectedValidatorReadBindings.RecordReader(services, descriptor);
        return services;
    }

    /// <summary>
    /// Resolves a read model for the current command context.
    /// </summary>
    /// <param name="readModelType">Type of read model to resolve.</param>
    /// <param name="commandContext">The <see cref="CommandContext"/> to resolve from.</param>
    /// <param name="readModels">The <see cref="IReadModels"/> service.</param>
    /// <param name="key">An optional per-parameter key, instead of the command key.</param>
    /// <returns>The resolved read model instance, or null when it does not exist.</returns>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">Thrown when the command context carries no usable event source id to resolve the read model by; it surfaces as a validation failure (HTTP 400).</exception>
    internal static object? ResolveReadModel(Type readModelType, CommandContext commandContext, IReadModels readModels, ReadModelKey? key = null)
    {
        if (PerStreamReadModelBinding.IsDeclared(readModelType))
        {
            return PerStreamReadModelBinding.Resolve(readModelType, commandContext, commandContext.ServiceProvider!);
        }
        var eventSourceId = key is null ? commandContext.GetEventSourceId() : (EventSourceId)key.Value;
        if (eventSourceId == EventSourceId.Unspecified)
        {
            // A read model is keyed by the command's event source id, so an unspecified id (the command carried no
            // usable key) can never resolve one — for a nullable and a non-nullable dependency alike. That is invalid
            // client input, not "the entity does not exist", so returning null would be misleading and letting the
            // throw fall through as-is would be an unhandled server error. UnableToResolveReadModelFromCommandContext
            // implements IValidationFailure, so the pipeline surfaces it as a validation failure (HTTP 400). A
            // valid-but-not-found read model still resolves to null below.
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }

        ReadProtectionMeasurement.Current?.StoredRead(readModelType, (ReadModelKey)eventSourceId);
        var readModel = readModels.GetInstanceById(readModelType, eventSourceId).GetAwaiter().GetResult();

        // A never-created or removed read model resolves to null; there is nothing to release (decrypt).
        // Hand back the null so command-side code can inject a nullable read model and treat null as
        // "does not exist".
        //
        // Whether there is anything to release is otherwise decided entirely by IReadModels.Release itself,
        // not by commandContext.GetSubject() here. The two answer different questions: GetSubject() finds
        // only an explicit ICanProvideSubject/[Subject]/Subject-typed member on the *command* - used to pick
        // the identity newly appended events are encrypted under - and returns null for the common case where
        // a command relies on the implicit event-source-id fallback instead. Release resolves its own subject
        // from the *read model instance* (its own [Subject] member, or its Id), which is unrelated to the
        // command's shape; and a namespace- or global-scoped [Encrypted] value needs no subject at all. Gating
        // release on the command's subject skipped it for most commands regardless of what the read model
        // actually carried.
        return readModel is null ? readModel : ReleaseReadModel(readModels, readModelType, readModel);
    }

    /// <summary>
    /// Resolves and releases a stored model without blocking command parameter binding.
    /// </summary>
    /// <param name="readModelType">The model type.</param>
    /// <param name="commandContext">The command invocation.</param>
    /// <param name="readModels">The stored model reader.</param>
    /// <param name="key">The parameter key.</param>
    /// <returns>The released model, or null.</returns>
    internal static async Task<object?> ResolveReadModelAsync(Type readModelType, CommandContext commandContext, IReadModels readModels, ReadModelKey? key = null)
    {
        var eventSourceId = key is null ? commandContext.GetEventSourceId() : (EventSourceId)key.Value;
        if (eventSourceId == EventSourceId.Unspecified)
        {
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }
        ReadProtectionMeasurement.Current?.StoredRead(readModelType, (ReadModelKey)eventSourceId);
        return await ReadModelDispatch.For(readModelType).ResolveStored(readModels, (ReadModelKey)eventSourceId);
    }

    /// <summary>
    /// Resolves a decision wrapper through typed asynchronous dispatch.
    /// </summary>
    /// <param name="readModelType">The wrapper type.</param>
    /// <param name="commandContext">The command invocation.</param>
    /// <param name="services">The invocation services.</param>
    /// <returns>The decision read.</returns>
    internal static Task<object?> ResolveDecisionReadAsync(Type readModelType, CommandContext commandContext, IServiceProvider services) =>
        ReadModelDispatch.For(readModelType.GetGenericArguments()[0]).ResolveDecisionRead(services, commandContext);

    /// <summary>
    /// Discovers the same Chronicle-backed models for parameter resolution and admission checks.
    /// </summary>
    /// <param name="artifacts">The discovered client artifacts.</param>
    /// <returns>The injectable read model types.</returns>
    internal static Type[] DiscoverReadModelTypes(IClientArtifactsProvider artifacts) =>
        _discoveryByArtifacts.GetValue(artifacts, _ => new ReadModelDiscovery()).GetTypes(artifacts);

    /// <summary>
    /// Registers typed dispatch for a read model type that <see cref="AddReadModels"/> did not discover. This and
    /// <see cref="AddReadModels"/> share the single place that closes the registration over the model type.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <returns>Whether dispatch is now registered.</returns>
    internal static bool TryRegisterDispatch(Type model)
    {
        if (!model.IsClass)
        {
            return false;
        }

        try
        {
            RegisterReadModel(model, null, []);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or TargetInvocationException)
        {
            return false;
        }
    }

    static void RegisterReadModel(Type model, IServiceCollection? services, Type[] readModelTypes) =>
        typeof(ReadModelServiceCollectionExtensions)
            .GetMethod(nameof(RegisterReadModelOf), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(model)
            .Invoke(null, [services, readModelTypes]);

    static void RegisterReadModelOf<T>(IServiceCollection? services, Type[] readModelTypes)
        where T : class
    {
        ReadModelDispatch.Register<T>();
        if (services is null)
        {
            return;
        }

        // Transient DI registration: the invocation cache lives in CommandDecisionReads, not in a reused provider.
        var descriptor = ServiceDescriptor.Transient(ResolveDecisionRead<T>);
        var count = services.Count;
        services.TryAdd(descriptor);
        if (services.Count != count)
        {
            ProtectedValidatorReadBindings.RecordToken(services, descriptor, readModelTypes);
        }
    }

    static DecisionRead<T> ResolveDecisionRead<T>(IServiceProvider services)
        where T : class => ResolveDecisionReadAsync<T>(services, services.GetRequiredService<ICommandContextAccessor>().Current).GetAwaiter().GetResult();

    /// <summary>
    /// Resolves a typed wrapper, preserving source and stream routing.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="services">The invocation services.</param>
    /// <param name="context">The command invocation.</param>
    /// <returns>The decision read.</returns>
    internal static async Task<DecisionRead<T>> ResolveDecisionReadAsync<T>(IServiceProvider services, CommandContext context)
        where T : class
    {
        if (PerStreamReadModelBinding.IsDeclared(typeof(T)))
        {
            var route = PerStreamReadModelBinding.Route(typeof(T), context, services);
            return await services.GetRequiredService<IDecisionReads>().Get<T>(context.GetEventSourceId(), route.EventStreamType, route.EventStreamId, route.EventSourceType, context.CancellationToken);
        }
        var key = context.GetEventSourceId();
        if (key == EventSourceId.Unspecified)
        {
            throw new UnableToResolveReadModelFromCommandContext(typeof(T));
        }

        return await services.GetRequiredService<IDecisionReads>().Get<T>((ReadModelKey)key, context.CancellationToken);
    }

    /// <summary>
    /// Extracts the read model target types behind a set of backing artifacts that implement a given open generic
    /// interface (for example <see cref="IProjectionFor{T}"/> or <see cref="IReducerFor{T}"/>).
    /// </summary>
    /// <param name="artifactTypes">The backing artifact types to inspect.</param>
    /// <param name="openGenericInterface">The open generic interface whose single type argument is the read model type.</param>
    /// <returns>The concrete read model types behind the artifacts.</returns>
    static IEnumerable<Type> ReadModelTargetsFrom(IEnumerable<Type> artifactTypes, Type openGenericInterface) =>
        artifactTypes
            .Select(artifactType => artifactType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                ?.GetGenericArguments()[0])
            .Where(type => type?.IsClass == true && !type.IsAbstract)
            .Cast<Type>();

    static object ReleaseReadModel(IReadModels readModels, Type readModelType, object readModel)
    {
        try
        {
            var task = (Task)_releaseMethod
                .MakeGenericMethod(readModelType)
                .Invoke(readModels, [readModel])!;

            task.GetAwaiter().GetResult();

            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to release read model '{readModelType.FullName}'.", exception);
        }
    }

    sealed class ReadModelDiscovery
    {
        readonly object _gate = new();
        Type[] _projections = [];
        Type[] _modelBoundProjections = [];
        Type[] _reducers = [];
        Type[] _readModelTypes = [];
        bool _initialized;

        public Type[] GetTypes(IClientArtifactsProvider artifacts)
        {
            // Artifact providers can be configured between registrations. Compare their actual type sets rather than
            // assuming that one provider instance always describes the same universe.
            var projections = artifacts.Projections.ToArray();
            var modelBoundProjections = artifacts.ModelBoundProjections.ToArray();
            var reducers = artifacts.Reducers.ToArray();
            lock (_gate)
            {
                if (!_initialized || !projections.SequenceEqual(_projections) ||
                    !modelBoundProjections.SequenceEqual(_modelBoundProjections) || !reducers.SequenceEqual(_reducers))
                {
                    var modelBoundReadModels = ModelBoundReadModelRoots.Discover(modelBoundProjections)
                        .Where(type => type.IsClass && !type.IsAbstract);

                    // Each backing kind contributes only read models that can be resolved by key through IReadModels.
                    _readModelTypes = ReadModelTargetsFrom(projections, typeof(IProjectionFor<>))
                        .Concat(modelBoundReadModels)
                        .Concat(ReadModelTargetsFrom(reducers, typeof(IReducerFor<>)))
                        .Concat(reducers.Where(SelfReducingReadModel.IsSelfReducing))
                        .Distinct()
                        .ToArray();
                    _projections = projections;
                    _modelBoundProjections = modelBoundProjections;
                    _reducers = reducers;
                    _initialized = true;
                }

                return _readModelTypes;
            }
        }
    }
}
