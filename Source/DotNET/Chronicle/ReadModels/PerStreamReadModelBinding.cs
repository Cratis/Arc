// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Binds a stream model to the same route used for the command's returned events, in every rollout mode.
/// </summary>
internal static class PerStreamReadModelBinding
{
    /// <summary>
    /// Gets whether a model declares one stream.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <returns>Whether the model has the Chronicle stream declaration.</returns>
    internal static bool IsDeclared(Type model) => Declaration(model) is not null;

    /// <summary>
    /// Resolves a plain stream model, including compliance release.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="context">The command invocation.</param>
    /// <param name="services">The command services.</param>
    /// <param name="parameter">The parameter, when resolving a parameter dependency.</param>
    /// <returns>The model, or null for an absent stream.</returns>
    /// <exception cref="ReadCannotBeProtected">The model cannot be protected.</exception>
    internal static object? Resolve(Type model, CommandContext context, IServiceProvider services, ParameterInfo? parameter = null)
    {
        try
        {
            return ReadModelDispatch.For(model).ResolveStream(context, services, parameter);
        }
        catch (DecisionReadRefused refused) when (parameter is not null)
        {
            throw ReadProtectionRefusals.For(context.Type, parameter, refused.Reason);
        }
    }

    /// <summary>
    /// Resolves a plain stream model without blocking, including compliance release.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <param name="context">The command invocation.</param>
    /// <param name="services">The command services.</param>
    /// <param name="parameter">The parameter, when resolving a parameter dependency.</param>
    /// <returns>The model, or null for an absent stream.</returns>
    /// <exception cref="ReadCannotBeProtected">The model cannot be protected.</exception>
    internal static async ValueTask<object?> ResolveAsync(Type model, CommandContext context, IServiceProvider services, ParameterInfo? parameter = null)
    {
        try
        {
            return await ReadModelDispatch.For(model).ResolveStreamAsync(context, services, parameter);
        }
        catch (DecisionReadRefused refused) when (parameter is not null)
        {
            throw ReadProtectionRefusals.For(context.Type, parameter, refused.Reason);
        }
    }

    /// <summary>
    /// Resolves and validates the command's route before any read is made.
    /// </summary>
    /// <param name="model">The stream model.</param>
    /// <param name="context">The command invocation.</param>
    /// <param name="services">The registered source definitions.</param>
    /// <param name="parameter">The dependency, if available.</param>
    /// <returns>The exact stream route.</returns>
    /// <exception cref="ReadModelStreamNotOnCommandRoute">The route does not select this stream.</exception>
    /// <exception cref="ReadModelStreamPartMissing">A template part is blank.</exception>
    /// <exception cref="InvalidLookUpBy">A stream model cannot be redirected.</exception>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">The command has no source id.</exception>
    internal static EventRoute Route(Type model, CommandContext context, IServiceProvider services, ParameterInfo? parameter = null)
    {
        var declaration = Declaration(model)!;
        var source = declaration.AttributeType.GenericTypeArguments[0];
        var stream = (string)declaration.ConstructorArguments[0].Value!;
        if (parameter?.GetCustomAttribute<LookUpByAttribute>() is { } lookup)
        {
            throw new InvalidLookUpBy(context.Type, lookup.Property, "the stream belongs to the command's own event source");
        }
        var definition = services.GetRequiredService<IEventSources>().GetFor(source);
        ReadModelStreamNotOnCommandRoute Refuse(string reason) => new(context.Type, parameter?.Name ?? model.Name, model, source, stream, reason);
        EventRoute route;
        try
        {
            route = context.GetEventRoute();
        }
        catch (EventStreamIdTemplatePartMissing missing)
        {
            if (missing.Property == "resolved stream id") throw Refuse("declares no stream id");
            throw new ReadModelStreamPartMissing(model, missing.Property);
        }
        if (route.EventSourceType != definition.EventSourceType || route.EventStreamType.Value != stream || route.EventStreamType.IsAll)
        {
            throw Refuse(route.EventStreamType.IsAll ? "declares no stream" : $"appends to {route.EventSourceType}/{route.EventStreamType}");
        }
        if (route.EventStreamId.IsDefault || route.EventStreamId == EventStreamId.NotSet || string.IsNullOrWhiteSpace(route.EventStreamId.Value))
        {
            throw Refuse("declares no stream id");
        }
        if (!context.GetEventSourceIdOrDefault().IsSpecified)
        {
            throw new UnableToResolveReadModelFromCommandContext(model);
        }
        return route;
    }

    /// <summary>
    /// Resolves the model with its concrete type, for typed dispatch.
    /// </summary>
    /// <typeparam name="T">The stream model type.</typeparam>
    /// <param name="context">The command invocation.</param>
    /// <param name="services">The command services.</param>
    /// <param name="parameter">The parameter, when resolving a parameter dependency.</param>
    /// <returns>The model, or null for an absent stream.</returns>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">The reader is not command aware.</exception>
    internal static T? ResolveModel<T>(CommandContext context, IServiceProvider services, ParameterInfo? parameter)
        where T : class
    {
        var plan = Plan<T>(context, services, parameter);
        if (plan.IsProtected)
        {
            var protectedInstance = plan.CommandReader!.GetStreamInstance<T>(plan.Source, plan.Route.EventStreamType, plan.Route.EventStreamId, plan.Route.EventSourceType, context.CancellationToken).GetAwaiter().GetResult();
            return parameter is null ? protectedInstance : (T?)ProtectedReadModelKeys.RequireInstance(protectedInstance, parameter, plan.KeyText);
        }
        using var allowance = Allow(plan.Advisory);
        var instance = StoredRead<T>(context, services, plan).GetAwaiter().GetResult();
        return instance is null ? null : services.GetRequiredService<IReadModels>().Release(instance).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resolves the model with its concrete type without blocking, for typed dispatch.
    /// </summary>
    /// <typeparam name="T">The stream model type.</typeparam>
    /// <param name="context">The command invocation.</param>
    /// <param name="services">The command services.</param>
    /// <param name="parameter">The parameter, when resolving a parameter dependency.</param>
    /// <returns>The model, or null for an absent stream.</returns>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">The reader is not command aware.</exception>
    internal static async ValueTask<object?> ResolveModelAsync<T>(CommandContext context, IServiceProvider services, ParameterInfo? parameter)
        where T : class
    {
        var plan = Plan<T>(context, services, parameter);
        if (plan.IsProtected)
        {
            var protectedInstance = await plan.CommandReader!.GetStreamInstance<T>(plan.Source, plan.Route.EventStreamType, plan.Route.EventStreamId, plan.Route.EventSourceType, context.CancellationToken);
            return parameter is null ? protectedInstance : ProtectedReadModelKeys.RequireInstance(protectedInstance, parameter, plan.KeyText);
        }
        using var allowance = Allow(plan.Advisory);
        var instance = await StoredRead<T>(context, services, plan);
        return instance is null ? null : await services.GetRequiredService<IReadModels>().Release(instance);
    }

    static StreamReadPlan Plan<T>(CommandContext context, IServiceProvider services, ParameterInfo? parameter)
        where T : class
    {
        var advisory = parameter?.GetCustomAttribute<UnprotectedAttribute>() ?? typeof(T).GetCustomAttribute<UnprotectedAttribute>();
        var keyText = string.Empty;
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null)
        {
            var raw = context.GetRawEventSourceKey();
            keyText = ProtectedReadModelKeys.Display(raw);
            ProtectedReadModelKeys.ValidateRawKey(ProtectedReadModelKeys.Traversal(services), typeof(T), raw, keyText);
            ProtectedReadModelKeys.ValidateWireText(typeof(T), context.GetEventSourceIdOrDefault().Value, keyText);
        }
        var route = Route(typeof(T), context, services, parameter);
        var source = context.GetEventSourceId();
        var reader = services.GetRequiredService<IDecisionReads>();
        var isProtected = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null;
        var commandReader = isProtected ? reader as CommandDecisionReads ?? throw new ProtectedCommandRequiresCommandAwareReader() : null;
        return new(advisory, keyText, route, source, reader, commandReader);
    }

    static IDisposable? Allow(UnprotectedAttribute? advisory) =>
        CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && TransactionalCommandScope.CurrentProtectedUnit is { } unit
            ? unit.AllowReads(ReadAllowance.Advisory, advisory?.Reason ?? "Declared advisory") : null;

    static Task<T> StoredRead<T>(CommandContext context, IServiceProvider services, StreamReadPlan plan)
        where T : class
    {
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ReadProtectionTelemetry.Record(context.Type, "advisory_read", typeof(T), plan.Advisory?.Reason ?? "Declared advisory");
        }
        ReadProtectionMeasurement.Current?.StreamRead<T>(plan.Reader, plan.Source, plan.Route.EventStreamType, plan.Route.EventStreamId, plan.Route.EventSourceType, plan.Advisory is not null);
        return services.GetRequiredService<IReadModels>().GetInstanceById<T>(plan.Source, plan.Route.EventStreamId);
    }


    static CustomAttributeData? Declaration(Type model) => model.GetCustomAttributesData().SingleOrDefault(attribute =>
        attribute.AttributeType.IsGenericType && attribute.AttributeType.GetGenericTypeDefinition() == typeof(EventStreamAttribute<>));

    sealed record StreamReadPlan(
        UnprotectedAttribute? Advisory,
        string KeyText,
        EventRoute Route,
        EventSourceId Source,
        IDecisionReads Reader,
        CommandDecisionReads? CommandReader)
    {
        public bool IsProtected => CommandReader is not null;
    }
}
