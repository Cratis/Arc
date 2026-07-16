// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
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
        ExecuteWithinScope(commands, null);

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, Type reactorType) =>
        ExecuteWithinScope(commands, EstablishSystemExecution(reactorType));

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

    async Task<Result<ReactorSideEffectFailure>> ExecuteWithinScope(IEnumerable<object> commands, IDisposable? systemExecutionScope)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var commandPipeline = scope.ServiceProvider.GetRequiredService<ICommandPipeline>();

        using (systemExecutionScope)
        {
            foreach (var command in commands)
            {
                var result = await commandPipeline.Execute(command, scope.ServiceProvider);
                if (!result.IsSuccess)
                {
                    return Result.Failed(CreateFailure(command, result));
                }
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
