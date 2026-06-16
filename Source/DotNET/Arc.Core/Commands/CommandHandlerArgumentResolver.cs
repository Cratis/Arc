// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandHandlerArgumentResolver"/>.
/// </summary>
/// <param name="provideInvoker">The <see cref="ICommandProvideInvoker"/> used to invoke the command's <c>Provide</c> method.</param>
[Singleton]
public class CommandHandlerArgumentResolver(ICommandProvideInvoker provideInvoker) : ICommandHandlerArgumentResolver
{
    /// <inheritdoc/>
    public async ValueTask<CommandHandlerArgumentResolution> Resolve(
        ICommandHandler handler,
        CommandContext context,
        IServiceProvider serviceProvider,
        ValidationResultSeverity? allowedSeverity)
    {
        var provided = await provideInvoker.Invoke(context, serviceProvider);
        var controlResult = CommandResult.Success(context.CorrelationId);
        var candidates = new List<object>();

        foreach (var value in provided)
        {
            if (IsControlSignal(value))
            {
                controlResult.MergeWith(ToCommandResult(value, context.CorrelationId));
            }
            else
            {
                candidates.Add(value);
            }
        }

        if (CommandValidationResults.IsBlocking(controlResult, allowedSeverity))
        {
            return new CommandHandlerArgumentResolution([], controlResult);
        }

        // Non-blocking control values (for example warnings) are dropped here, just as the pipeline drops
        // them when filtering by severity, so the resolution reports a clean success.
        var arguments = BuildArguments(handler, candidates, serviceProvider);
        return new CommandHandlerArgumentResolution(arguments, CommandResult.Success(context.CorrelationId));
    }

    static object?[] BuildArguments(ICommandHandler handler, List<object> candidates, IServiceProvider serviceProvider)
    {
        var parameters = handler.Parameters.ToArray();
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;
            var match = candidates.Find(parameterType.IsInstanceOfType);
            if (match is not null)
            {
                arguments[i] = match;
                candidates.Remove(match);
            }
            else
            {
                arguments[i] = ParameterDependencyResolver.Resolve(
                    serviceProvider,
                    parameter,
                    _ => new CannotResolveCommandDependency(parameter));
            }
        }

        return arguments;
    }

    static bool IsControlSignal(object value) =>
        value is CommandResult or ValidationResult or IEnumerable<ValidationResult> or AuthorizationResult;

    static CommandResult ToCommandResult(object value, CorrelationId correlationId) => value switch
    {
        CommandResult commandResult => commandResult,
        ValidationResult validationResult => new() { CorrelationId = correlationId, ValidationResults = [validationResult] },
        IEnumerable<ValidationResult> validationResults => new() { CorrelationId = correlationId, ValidationResults = [.. validationResults] },
        AuthorizationResult { IsAuthorized: false } authorizationResult => CommandResult.Unauthorized(correlationId, authorizationResult.FailureReason),
        _ => CommandResult.Success(correlationId)
    };
}
