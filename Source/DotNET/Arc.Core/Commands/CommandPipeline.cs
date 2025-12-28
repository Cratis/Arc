// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandPipeline"/>.
/// </summary>
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> to use for accessing correlation IDs.</param>
/// <param name="commandFilters">The <see cref="ICommandFilters"/> to use for filtering commands.</param>
/// <param name="handlerProviders">The <see cref="ICommandHandlerProviders"/> to use for finding command handlers.</param>
/// <param name="valueHandlers">The <see cref="ICommandResponseValueHandlers"/> to use for handling response values.</param>
/// <param name="contextModifier">The <see cref="ICommandContextModifier"/> to use for setting the current command context.</param>
/// <param name="contextValuesBuilder">The <see cref="ICommandContextValuesBuilder"/> to use for building command context values.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use for resolving dependencies.</param>
[Singleton]
public class CommandPipeline(
    ICorrelationIdAccessor correlationIdAccessor,
    ICommandFilters commandFilters,
    ICommandHandlerProviders handlerProviders,
    ICommandResponseValueHandlers valueHandlers,
    ICommandContextModifier contextModifier,
    ICommandContextValuesBuilder contextValuesBuilder,
    IServiceProvider serviceProvider) : ICommandPipeline
{
    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command)
    {
        var correlationId = GetCorrelationId();
        var result = CommandResult.Success(correlationId);
        try
        {
            handlerProviders.TryGetHandlerFor(command, out var commandHandler);
            if (commandHandler is null)
            {
                return CommandResult.MissingHandler(correlationId, command.GetType());
            }

            var dependencies = commandHandler.Dependencies.Select(serviceProvider.GetRequiredService);
            var commandContext = new CommandContext(
                correlationId,
                command.GetType(),
                command,
                dependencies,
                contextValuesBuilder.Build(command));
            contextModifier.SetCurrent(commandContext);
            result = await commandFilters.OnExecution(commandContext);
            if (!result.IsSuccess)
            {
                return result;
            }

            var response = await commandHandler.Handle(commandContext);
            if (response is not null)
            {
                var processedResult = await ProcessResponseValue(response, commandContext, correlationId, result);
                commandContext = processedResult.CommandContext;
                result = processedResult.Result;
            }
        }
        catch (Exception ex)
        {
            result.MergeWith(CommandResult.Error(correlationId, ex));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Validate(object command)
    {
        var correlationId = GetCorrelationId();
        var result = CommandResult.Success(correlationId);
        try
        {
            handlerProviders.TryGetHandlerFor(command, out var commandHandler);
            if (commandHandler is null)
            {
                return CommandResult.MissingHandler(correlationId, command.GetType());
            }

            var dependencies = commandHandler.Dependencies.Select(serviceProvider.GetRequiredService);
            var commandContext = new CommandContext(
                correlationId,
                command.GetType(),
                command,
                dependencies,
                contextValuesBuilder.Build(command));
            contextModifier.SetCurrent(commandContext);

            // Run only filters (authorization and validation), skip handler execution
            result = await commandFilters.OnExecution(commandContext);
        }
        catch (Exception ex)
        {
            result.MergeWith(CommandResult.Error(correlationId, ex));
        }

        return result;
    }

    object UnwrapValue(object value)
    {
        return value switch
        {
            IOneOf oneOf => UnwrapValue(oneOf.Value),
            _ => value
        };
    }

    bool CanHandleValue(object value, CommandContext commandContext)
    {
        return value switch
        {
            IOneOf oneOf => CanHandleOneOfValue(oneOf, commandContext),
            ITuple tuple => CanHandleTuple(tuple, commandContext),
            _ => valueHandlers.CanHandle(commandContext, value)
        };
    }

    bool CanHandleOneOfValue(IOneOf oneOf, CommandContext commandContext)
    {
        if (valueHandlers.CanHandle(commandContext, oneOf))
        {
            return true;
        }

        return CanHandleValue(oneOf.Value, commandContext);
    }

    bool CanHandleTuple(ITuple tuple, CommandContext commandContext)
    {
        for (var i = 0; i < tuple.Length; i++)
        {
            var element = tuple[i];
            if (element is null)
            {
                continue;
            }

            if (!CanHandleValue(element, commandContext))
            {
                return false;
            }
        }

        return true;
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessResponseValue(
        object response,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        return response switch
        {
            ITuple tuple => await ProcessTupleResponse(tuple, commandContext, correlationId, result),
            IOneOf oneOf => await ProcessOneOfResponse(oneOf, commandContext, correlationId, result),
            _ => await ProcessSimpleResponse(response, commandContext, correlationId, result)
        };
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessTupleResponse(
        ITuple tuple,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var values = ExtractValuesFromTupleInOrder(tuple);

        // First pass: identify and set the response value
        // We need to do this before handling other values because handlers may depend on the response being set
        var valuesToProcess = new List<object>();
        object? responseValue = null;

        foreach (var value in values)
        {
            if (CanHandleValue(value, commandContext))
            {
                valuesToProcess.Add(value);
            }
            else
            {
                var unwrappedValue = UnwrapValue(value);
                if (responseValue is null)
                {
                    responseValue = unwrappedValue;
                    commandContext = commandContext with { Response = unwrappedValue };
                    result = CreateCommandResultWithResponse(correlationId, unwrappedValue);
                }
                else
                {
                    // Multiple unhandled values - can't determine which should be the response
                    throw new MultipleUnhandledTupleValues([responseValue, unwrappedValue]);
                }
            }
        }

        // Second pass: now handle all handleable values with response set in context
        // Some handlers may need to re-check if they can handle based on the response now being set
        foreach (var value in valuesToProcess)
        {
            if (CanHandleValue(value, commandContext))
            {
                result.MergeWith(await HandleValue(value, commandContext));
            }
            else
            {
                // This value was handleable in the first pass but is no longer handleable
                // This should not happen in practice, but handle it defensively
                throw new MultipleUnhandledTupleValues([responseValue ?? UnwrapValue(value), UnwrapValue(value)]);
            }
        }

        return (commandContext, result);
    }

    IEnumerable<object> ExtractValuesFromTupleInOrder(ITuple tuple)
    {
        for (var i = 0; i < tuple.Length; i++)
        {
            if (tuple[i] is not null)
            {
                yield return tuple[i]!;
            }
        }
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessOneOfResponse(
        IOneOf oneOf,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var innerValue = oneOf.Value;
        return await ProcessResponseValue(innerValue, commandContext, correlationId, result);
    }

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessSimpleResponse(
        object response,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        if (valueHandlers.CanHandle(commandContext, response))
        {
            result.MergeWith(await valueHandlers.Handle(commandContext, response));
        }
        else
        {
            commandContext = commandContext with { Response = response };
            result = CreateCommandResultWithResponse(correlationId, response);
        }

        return (commandContext, result);
    }

    async Task<CommandResult> HandleValue(object value, CommandContext commandContext)
    {
        return value switch
        {
            ITuple tuple => await HandleTuple(tuple, commandContext),
            IOneOf oneOf => await HandleValue(oneOf.Value, commandContext),
            _ => await valueHandlers.Handle(commandContext, value)
        };
    }

    async Task<CommandResult> HandleTuple(ITuple tuple, CommandContext commandContext)
    {
        var result = CommandResult.Success(commandContext.CorrelationId);

        for (var i = 0; i < tuple.Length; i++)
        {
            var element = tuple[i];
            if (element is null)
            {
                continue;
            }

            result.MergeWith(await HandleValue(element, commandContext));
        }

        return result;
    }

    CorrelationId GetCorrelationId()
    {
        var correlationId = correlationIdAccessor.Current;
        if (correlationId == CorrelationId.NotSet)
        {
            correlationId = CorrelationId.New();
        }

        return correlationId;
    }

    CommandResult CreateCommandResultWithResponse(CorrelationId correlationId, object response)
    {
        var commandResultType = typeof(CommandResult<>).MakeGenericType(response.GetType());
        return (Activator.CreateInstance(commandResultType, correlationId, response) as CommandResult)!;
    }
}
