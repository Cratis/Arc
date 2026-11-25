// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

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
        using var batch = ParameterDependencyBatch.Begin(serviceProvider, handler.Parameters.ToArray());
        var provided = await provideInvoker.Invoke(context, serviceProvider);
        ValidateProvided(provided, serviceProvider);

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

        if (CommandValidationResults.IsBlocking(controlResult, context.BlockUnknownValidationSeverity ? context.AllowedSeverity : allowedSeverity, context.BlockUnknownValidationSeverity))
        {
            return new CommandHandlerArgumentResolution([], controlResult);
        }

        // Non-blocking control values (for example warnings) are dropped here, just as the pipeline drops
        // them when filtering by severity, so the resolution reports a clean success.
        var arguments = await BuildArguments(new(handler, context, serviceProvider, batch), candidates);
        return new CommandHandlerArgumentResolution(arguments, CommandResult.Success(context.CorrelationId));
    }

    static async Task<object?[]> BuildArguments(ArgumentBinding binding, List<object> candidates)
    {
        foreach (var parameter in binding.Batch.Parameters)
        {
            binding.Batch.Set(parameter, await BindArgument(binding, candidates, parameter));
        }
        return binding.Batch.Arguments;
    }

    static async ValueTask<object?> BindArgument(ArgumentBinding binding, List<object> candidates, ParameterInfo parameter)
    {
        var (handler, context, services, batch) = binding;
        if (parameter.IsDefined(typeof(GeneratedValueAttribute), inherit: false))
        {
            return await GeneratedValueResolution.Resolve(handler.CommandType, parameter, context, services);
        }
        if (parameter.ParameterType == typeof(CancellationToken)) return context.CancellationToken;
        var match = candidates.Find(parameter.ParameterType.IsInstanceOfType);
        if (match is not null) RefuseProvideForCallerKeyedParameter(handler, parameter);
        if (match is not null && !batch.RequiresAcquisition(parameter))
        {
            ValidateDependency(services, parameter, match);
            candidates.Remove(match);
            return match;
        }
        return await ParameterDependencyResolver.ResolveAsync(services, parameter, _ => new CannotResolveCommandDependency(parameter));
    }

    static void ValidateDependency(IServiceProvider services, ParameterInfo parameter, object value)
    {
        if (CommandDecisionPolicy.IsProtected)
        {
            services.GetRequiredService<ICommandProtectedDecisionSupport>().ValidateCommandDependency(parameter.ParameterType, value);
        }
    }

    static void ValidateProvided(IReadOnlyList<object> provided, IServiceProvider services)
    {
        // Legacy commands keep their existing Provide semantics.
        if (provided.Count == 0 || CommandDecisionPolicy.Mode == CommandDecisionMode.Legacy) return;
        if (services.GetService(typeof(IEnumerable<ICommandDependencySafety>)) is not IEnumerable<ICommandDependencySafety> safetyChecks) return;
        var checks = safetyChecks.ToArray();
        foreach (var value in provided)
        {
            foreach (var safety in checks) safety.ValidateProvided(value);
        }
    }

    static void RefuseProvideForCallerKeyedParameter(ICommandHandler handler, ParameterInfo parameter)
    {
        if (LookUpByKeyProviderDeclarations.For(parameter).FirstOrDefault(LookUpByKeyProviderDeclarations.IsProvider) is ILookUpByKeyProvider provider)
        {
            throw new InvalidLookUpBy(
                handler.CommandType,
                provider.ProviderType,
                "a value returned from Provide() cannot replace a caller-keyed read model; remove it from Provide() or drop the lookup");
        }
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

    sealed record ArgumentBinding(ICommandHandler Handler, CommandContext Context, IServiceProvider Services, ParameterDependencyBatch Batch);
}
