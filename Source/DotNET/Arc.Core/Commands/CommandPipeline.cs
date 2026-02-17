// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;
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
[Singleton]
public class CommandPipeline(
    ICorrelationIdAccessor correlationIdAccessor,
    ICommandFilters commandFilters,
    ICommandHandlerProviders handlerProviders,
    ICommandResponseValueHandlers valueHandlers,
    ICommandContextModifier contextModifier,
    ICommandContextValuesBuilder contextValuesBuilder) : ICommandPipeline
{
    /// <inheritdoc/>
    public async Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default)
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
                contextValuesBuilder.Build(command),
                allowedSeverity);
            contextModifier.SetCurrent(commandContext);
            result = await commandFilters.OnExecution(commandContext);
            result = FilterValidationResults(result, allowedSeverity);
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
    public async Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default)
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
                contextValuesBuilder.Build(command),
                allowedSeverity);
            contextModifier.SetCurrent(commandContext);

            // Run only filters (authorization and validation), skip handler execution
            result = await commandFilters.OnExecution(commandContext);
            result = FilterValidationResults(result, allowedSeverity);
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
        var values = ExtractValuesFromTupleInOrder(tuple).ToList();

        // First pass: identify and set the response value
        // Handlers may check commandContext.Response, so we need to set it before checking possibility to handle
        var valuesToProcess = new List<object>();
        object? responseValue = null;

        foreach (var value in values)
        {
            // Check if this value should be the response (non-handled value)
            // We need to check this WITHOUT the response being set yet
            if (!CanHandleValue(value, commandContext))
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

        // Second pass: now that response is set, check which values can be handled
        // Some handlers may depend on commandContext.Response being set
        foreach (var value in values)
        {
            if (CanHandleValue(value, commandContext))
            {
                valuesToProcess.Add(value);
            }
        }

        // Third pass: handle all values that can be handled
        foreach (var value in valuesToProcess)
        {
            result.MergeWith(await HandleValue(value, commandContext));
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

    CommandResult FilterValidationResults(CommandResult result, ValidationResultSeverity? allowedSeverity)
    {
        if (allowedSeverity is null)
        {
            // Default behavior: only allow errors through (block warnings and information)
            result.ValidationResults = result.ValidationResults.Where(v => v.Severity == ValidationResultSeverity.Error).ToArray();
        }
        else
        {
            // Filter out validation results with severity <= allowedSeverity
            result.ValidationResults = result.ValidationResults.Where(v => v.Severity > allowedSeverity).ToArray();
        }

        return result;
    }

    CommandResult CreateCommandResultWithResponse(CorrelationId correlationId, object response)
    {
        var commandResultType = typeof(CommandResult<>).MakeGenericType(response.GetType());
        return (Activator.CreateInstance(commandResultType, correlationId, response) as CommandResult)!;
    }
}
