// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Registers source-generated typed operation invokers. Source-free dynamic commands use a validated reflection fallback.
/// This registry does not imply that the rest of Arc supports NativeAOT.
/// </summary>
public static class CommandOperationInvokers
{
    static readonly ConcurrentDictionary<Type, CommandOperationInvoker> _invokers = new();

    /// <summary>
    /// Registers typed metadata emitted by the Arc source generator.
    /// </summary>
    /// <param name="operationType">The concrete declaration type.</param>
    /// <param name="invoker">The generated call metadata.</param>
    public static void Register(Type operationType, CommandOperationInvoker invoker) => _invokers[operationType] = invoker;

    /// <summary>
    /// Gets typed metadata or validates a source-free declaration on demand.
    /// </summary>
    /// <param name="type">The concrete operation type.</param>
    /// <returns>Validated call metadata.</returns>
    internal static CommandOperationInvoker Get(Type type) => _invokers.GetOrAdd(type, Create);

    static CommandOperationInvoker Create(Type type)
    {
        var execute = FindMethod(type, "Execute", required: true)!;
        var compensate = FindMethod(type, "Compensate", required: false);

        return new(
            execute.GetParameters().Select(parameter => parameter.ParameterType),
            (operation, arguments) => Invoke(execute, operation, arguments),
            compensate?.GetParameters().Select(parameter => parameter.ParameterType) ?? [],
            compensate is null ? null : (operation, arguments) => Invoke(compensate, operation, arguments));
    }

    static MethodInfo? FindMethod(Type type, string name, bool required)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == name).ToArray();
        if (methods.Length == 0 && !required)
        {
            return null;
        }

        if (methods.Length != 1)
        {
            throw new InvalidCommandOperation($"Operation '{type}' must have {(required ? "exactly one" : "at most one")} public instance {name} method.");
        }

        var method = methods[0];
        var parameters = method.GetParameters();
        if (!method.IsPublic || method.IsStatic || method.IsGenericMethod || method.IsAbstract ||
            (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task) && method.ReturnType != typeof(ValueTask)) ||
            (method.ReturnType == typeof(void) && method.IsDefined(typeof(AsyncStateMachineAttribute))) ||
            parameters.Any(parameter => parameter.ParameterType.IsByRef || parameter.ParameterType.IsPointer || parameter.IsOptional || parameter.IsDefined(typeof(ParamArrayAttribute)) ||
                typeof(IServiceProvider).IsAssignableFrom(parameter.ParameterType) ||
                typeof(IServiceScopeFactory).IsAssignableFrom(parameter.ParameterType) ||
                typeof(IServiceScope).IsAssignableFrom(parameter.ParameterType) ||
                (name == "Execute" && parameter.ParameterType == typeof(CommandOperationFailure))) ||
            parameters.Count(parameter => parameter.ParameterType == typeof(CancellationToken)) > 1 ||
            parameters.Count(parameter => parameter.ParameterType == typeof(CommandOperationFailure)) > 1)
        {
            throw new InvalidCommandOperation($"Operation '{type}.{name}' requires a public nongeneric instance method returning void, Task, or ValueTask with required service parameters, one optional CancellationToken parameter, and failure context only on Compensate. Async void, service locators, and ref parameters are unsupported.");
        }

        return method;
    }

    static async ValueTask Invoke(MethodInfo method, ICommandOperation operation, object?[] arguments)
    {
        try
        {
            var returned = method.Invoke(operation, arguments);
            if (returned is null && method.ReturnType != typeof(void))
            {
                throw new InvalidCommandOperation($"Operation '{operation.GetType()}.{method.Name}' returned a null awaitable.");
            }

            switch (returned)
            {
                case Task task:
                    await task;
                    break;
                case ValueTask task:
                    await task;
                    break;
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
