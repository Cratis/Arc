// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
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
            return typeof(PerStreamReadModelBinding).GetMethod(nameof(ResolveModel), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(model).Invoke(null, [context, services, parameter]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            if (exception.InnerException is DecisionReadRefused refused && parameter is not null)
            {
                throw ReadProtectionRefusals.For(context.Type, parameter, refused.Reason);
            }
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
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

    static CustomAttributeData? Declaration(Type model) => model.GetCustomAttributesData().SingleOrDefault(attribute =>
        attribute.AttributeType.IsGenericType && attribute.AttributeType.GetGenericTypeDefinition() == typeof(EventStreamAttribute<>));

    static T? ResolveModel<T>(CommandContext context, IServiceProvider services, ParameterInfo? parameter)
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
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && advisory is null)
        {
            var commandReader = reader as CommandDecisionReads ?? throw new ProtectedCommandRequiresCommandAwareReader();
            var protectedInstance = commandReader.GetStreamInstance<T>(source, route.EventStreamType, route.EventStreamId, route.EventSourceType, context.CancellationToken).GetAwaiter().GetResult();
            return parameter is null ? protectedInstance : (T?)ProtectedReadModelKeys.RequireInstance(protectedInstance, parameter, keyText);
        }
        using var allowance = CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && TransactionalCommandScope.CurrentProtectedUnit is { } unit
            ? unit.AllowReads(ReadAllowance.Advisory, advisory?.Reason ?? "Declared advisory") : null;
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ReadProtectionTelemetry.Record(context.Type, "advisory_read", typeof(T), advisory?.Reason ?? "Declared advisory");
        }
        ReadProtectionMeasurement.Current?.StreamRead<T>(reader, source, route.EventStreamType, route.EventStreamId, route.EventSourceType, advisory is not null);
        var models = services.GetRequiredService<IReadModels>();
        var instance = models.GetInstanceById<T>(source, route.EventStreamId).GetAwaiter().GetResult();
        return instance is null ? null : models.Release(instance).GetAwaiter().GetResult();
    }
}
