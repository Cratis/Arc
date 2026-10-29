// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="ICommandSideEffectExecutor"/> that executes commands through
/// the <see cref="ICommandPipeline"/> within a dedicated service scope.
/// </summary>
/// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory"/> used to create a scope per execution.</param>
/// <param name="systemExecution">The <see cref="ISystemExecution"/> used to execute commands as a system actor when the reactor declares roles.</param>
[Singleton]
public class CommandSideEffectExecutor(IServiceScopeFactory serviceScopeFactory, ISystemExecution systemExecution) : ICommandSideEffectExecutor
{
    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands) =>
        ExecuteWithinScope(commands, null, null);

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, Type reactorType) =>
        ExecuteWithinScope(commands, reactorType, null);

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, ReactorContext reactorContext, IEventStore eventStore) =>
        ExecuteWithinScope(commands, reactorContext.Reactor.GetType(), (value, commandIndex) => RefusalEvents.Append(reactorContext, eventStore, value, commandIndex));

    static ReactorSideEffectFailure CreateFailure(object command, CommandResult result) =>
        new([new AppendFailure([], false, DescribeFailure(command.GetType(), result), [])]);

    static IEnumerable<string> DescribeFailure(Type commandType, CommandResult result)
    {
        var name = commandType.Name;
        if (!result.IsAuthorized)
        {
            yield return $"Command '{name}' was not authorized. {result.AuthorizationFailureReason}".TrimEnd();
        }

        foreach (var validationResult in result.ValidationResults)
        {
            yield return $"Command '{name}' failed validation: {validationResult.Message}";
        }

        foreach (var exceptionMessage in result.ExceptionMessages)
        {
            yield return $"Command '{name}' threw an exception: {exceptionMessage}";
        }
    }

    static async Task<Result<ReactorSideEffectFailure>> HandleFailure(object command, CommandResult result, CommandInvocation? invocation, Func<object, Task<Result<ReactorSideEffectFailure>>>? append)
    {
        if (invocation is null)
        {
            return Result.Failed(CreateFailure(command, result));
        }

        var refusal = result.GetRefusal();
        var branch = refusal is not null ? invocation.FindBranch(refusal) : null;
        if (branch is null)
        {
            return Result.Failed(CreateFailure(command, result));
        }

        var events = branch.Handler(refusal!);
        if (events is null)
        {
            return Result.Success<ReactorSideEffectFailure>();
        }

        return append is not null ? await append(events) : Result.Failed(CreateFailure(command, result));
    }

    async Task<Result<ReactorSideEffectFailure>> ExecuteWithinScope(IEnumerable<object> commands, Type? reactorType, Func<object, int, Task<Result<ReactorSideEffectFailure>>>? append)
    {
        using var systemExecutionScope = reactorType is not null ? EstablishSystemExecution(reactorType) : null;
        using var scope = serviceScopeFactory.CreateScope();
        var commandPipeline = scope.ServiceProvider.GetRequiredService<ICommandPipeline>();

        foreach (var (item, commandIndex) in commands.Select((item, index) => (item, index)))
        {
            var invocation = item as CommandInvocation;
            var command = invocation?.Command ?? item;
            var result = await commandPipeline.Execute(command, scope.ServiceProvider);
            if (!result.IsSuccess)
            {
                return await HandleFailure(command, result, invocation, append is not null ? events => append(events, commandIndex) : null);
            }
        }

        return Result.Success<ReactorSideEffectFailure>();
    }

    IDisposable? EstablishSystemExecution(Type reactorType)
    {
        var attribute = reactorType.GetCustomAttribute<ExecuteCommandsAsSystemAttribute>();
        return attribute is not null ? systemExecution.AsSystem([.. attribute.Roles]) : null;
    }
}
