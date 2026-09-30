// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Observability;
using Cratis.DependencyInjection;
using Cratis.Tasks;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandProvideInvoker"/>.
/// </summary>
[Singleton]
public class CommandProvideInvoker : ICommandProvideInvoker
{
    readonly ConcurrentDictionary<Type, MethodInfo?> _provideMethodsByCommandType = new();

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<object>> Invoke(CommandContext context, IServiceProvider serviceProvider)
    {
        var provideMethod = _provideMethodsByCommandType.GetOrAdd(context.Type, type => type.GetProvideMethod());
        if (provideMethod is null)
        {
            return [];
        }

        using var span = BeginProvide(context.Type, serviceProvider);
        try
        {
            var arguments = ResolveArguments(context, provideMethod, serviceProvider);
            var invocationResult = Invoke(provideMethod, context.Command, arguments);
            var (_, value) = await AwaitableHelpers.AwaitIfNeeded(invocationResult);
            return Flatten(value);
        }
        catch (Exception ex)
        {
            OperationActivity.RecordException(span?.Activity, ex);
            span?.Activity?.SetStatus(ActivityStatusCode.Error, OperationOutcomes.Error);
            throw;
        }
    }

    static IActivityScope<CommandProvideInvoker>? BeginProvide(Type commandType, IServiceProvider serviceProvider)
    {
        var commandTypeName = commandType.FullName ?? commandType.Name;
        var span = serviceProvider.GetService<IActivitySource<CommandProvideInvoker>>()?.Provide(commandTypeName);
        if (span?.Activity is { IsAllDataRequested: true } activity)
        {
            activity.DisplayName = $"{OperationActivity.ShortNameOf(commandType)}.Provide()";
            activity.SetTag(TelemetryNames.CommandType, commandTypeName);
        }

        return span;
    }

    static object?[]? ResolveArguments(CommandContext context, MethodInfo provideMethod, IServiceProvider serviceProvider)
    {
        var parameters = provideMethod.GetParameters();
        return parameters.Length == 0
            ? null
            : parameters.Select(parameter => ResolveArgument(context, serviceProvider, parameter)).ToArray();
    }

    static object? ResolveArgument(CommandContext context, IServiceProvider serviceProvider, ParameterInfo parameter) =>
        parameter.ParameterType == typeof(CancellationToken)
            ? context.CancellationToken
            : ParameterDependencyResolver.Resolve(
                serviceProvider,
                parameter,
                _ => new CannotResolveCommandDependency(parameter));

    static object? Invoke(MethodInfo provideMethod, object command, object?[]? arguments)
    {
        try
        {
            return provideMethod.Invoke(command, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    static List<object> Flatten(object? value)
    {
        value = Unwrap(value);
        if (value is null)
        {
            return [];
        }

        if (value is ITuple tuple)
        {
            var values = new List<object>(tuple.Length);
            for (var i = 0; i < tuple.Length; i++)
            {
                if (Unwrap(tuple[i]) is { } element)
                {
                    values.Add(element);
                }
            }

            return values;
        }

        return [value];
    }

    static object? Unwrap(object? value) => value switch
    {
        IOneOf oneOf => Unwrap(oneOf.Value),
        _ => value
    };
}
