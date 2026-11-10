// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Authorization;
using Cratis.Arc.Validation;
using Cratis.Execution;
using OneOf;

namespace Cratis.Arc.Commands;

public partial class CommandPipeline
{
    bool IsOperationControl(object value) => value is CommandResult or ValidationResult or AuthorizationResult or IEnumerable<ValidationResult>;

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

    async Task<(CommandContext CommandContext, CommandResult Result)> ProcessOperationResponse(
        object[] values,
        CommandContext commandContext,
        CorrelationId correlationId,
        CommandResult result)
    {
        var ordinary = values.Where(value => value is not ICommandOperation and not CommandOperations).ToArray();
        foreach (var value in ordinary)
        {
            valueHandlers.UpdateContext(commandContext, value);
        }

        var (context, response) = SelectOperationResponse(ordinary, commandContext);
        if (response is not null)
        {
            var withResponse = CreateCommandResultWithResponse(correlationId, response);
            withResponse.MergeWith(result);
            result = withResponse;
        }

        await MergeControlValues(ordinary, context, result);

        result = FilterValidationResults(result, context);
        if (result.IsSuccess)
        {
            await HandleRemainingValues(ordinary, response, context, result);
        }

        return (context, result);
    }

    (CommandContext Context, object? Response) SelectOperationResponse(object[] ordinary, CommandContext commandContext)
    {
        object? response = null;
        foreach (var value in ordinary.Where(value => !IsOperationControl(value)))
        {
            if (valueHandlers.CanHandle(commandContext, value))
            {
                continue;
            }

            if (response is not null)
            {
                throw new MultipleUnhandledTupleValues([response, value]);
            }

            response = value;
            commandContext = commandContext with { Response = value };
        }

        return (commandContext, response);
    }

    async Task MergeControlValues(object[] ordinary, CommandContext commandContext, CommandResult result)
    {
        // Control values always precede events/other handlers, regardless of tuple order. Operations are exclusively
        // owned here, so even a broad custom response handler never sees or executes an operation declaration.
        foreach (var value in ordinary.Where(IsOperationControl))
        {
            result.MergeWith(value is CommandResult control ? control : await valueHandlers.Handle(commandContext, value));
        }
    }

    async Task HandleRemainingValues(object[] ordinary, object? response, CommandContext commandContext, CommandResult result)
    {
        foreach (var value in ordinary.Where(value => !IsOperationControl(value) && !ReferenceEquals(value, response)))
        {
            result.MergeWith(await valueHandlers.Handle(commandContext, value));
            if (!result.IsSuccess)
            {
                break;
            }
        }
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
            IOneOf oneOf => await ProcessResponseValue(oneOf.Value, commandContext, correlationId, result),
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

        foreach (var value in values)
        {
            valueHandlers.UpdateContext(commandContext, value);
        }

        // First pass: identify and set the response value
        // Handlers may check commandContext.Response, so we need to set it before checking possibility to handle
        object? responseValue = null;

        foreach (var value in values)
        {
            // Check if this value should be the response (non-handled value)
            // We need to check this WITHOUT the response being set yet
            if (CanHandleValue(value, commandContext))
            {
                continue;
            }

            var unwrappedValue = UnwrapValue(value);
            if (responseValue is not null)
            {
                // Multiple unhandled values - can't determine which should be the response
                throw new MultipleUnhandledTupleValues([responseValue, unwrappedValue]);
            }

            responseValue = unwrappedValue;
            commandContext = commandContext with { Response = unwrappedValue };
            result = CreateCommandResultWithResponse(correlationId, unwrappedValue);
        }

        // Second pass: now that response is set, check which values can be handled
        // Some handlers may depend on commandContext.Response being set
        // Third pass: handle all values that can be handled
        foreach (var value in values.Where(value => CanHandleValue(value, commandContext)).ToList())
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
}
