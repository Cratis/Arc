// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that validates commands before they are handled.
/// </summary>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for finding validators.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> used to log a validator that throws while validating.</param>
public class FluentValidationFilter(IDiscoverableValidators discoverableValidators, ILogger<FluentValidationFilter> logger) : ICommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var commandResult = CommandResult.Success(context.CorrelationId);
        commandResult.MergeWith(await Validate(context, context.Command, new HashSet<object>(ReferenceEqualityComparer.Instance)));
        return commandResult;
    }

    async Task<CommandResult> Validate(CommandContext context, object instance, HashSet<object> visited)
    {
        var commandResult = CommandResult.Success(context.CorrelationId);

        var instanceType = instance.GetType();

        // Guard against cycles in arbitrary object graphs. Some types — notably
        // System.Text.Json.Nodes.JsonNode — hold a back-reference from every child to its parent, so
        // blindly walking child properties would recurse forever and overflow the stack. Only reference
        // types can participate in a cycle; value types are boxed afresh on each access, so tracking them
        // would never dedupe and would only add overhead. ReferenceEqualityComparer keys on identity, so
        // distinct-but-equal instances (e.g. two equal concept values in a list) are still each validated.
        if (!instanceType.IsValueType && !visited.Add(instance))
        {
            return commandResult;
        }

        if (TryGetValidator(context, instanceType, out var validator))
        {
            commandResult.MergeWith(await RunValidator(context, instance, validator));
        }

        if (!instanceType.IsPrimitive &&
            instanceType != typeof(string) &&
            instanceType != typeof(DateTime) &&
            instanceType != typeof(DateTimeOffset) &&
            instanceType != typeof(Guid) &&
            instanceType != typeof(decimal))
        {
            if (instanceType.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(instanceType))
            {
                foreach (var element in (System.Collections.IEnumerable)instance)
                {
                    if (element is null) continue;
                    commandResult.MergeWith(await Validate(context, element, visited));
                }
            }
            else
            {
                foreach (var property in instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // Skip indexer properties — they require index arguments, so GetValue(instance)
                    // without any would throw "Parameter count mismatch". These show up on types such as
                    // JsonElement (this[int]) that can appear in an object-typed command property graph.
                    if (property.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    var propertyValue = property.GetValue(instance);
                    if (propertyValue is not null)
                    {
                        commandResult.MergeWith(await Validate(context, propertyValue, visited));
                    }
                }
            }
        }

        return commandResult;
    }

    /// <summary>
    /// Runs a resolved validator against an instance, converting a validator that throws while validating
    /// (for example a rule that dereferences a null concept member such as <c>RuleFor(c =&gt; c.X.Value)</c> on a
    /// null <c>X</c>) into a validation failure (HTTP 400) instead of letting it propagate as a server error (HTTP 500).
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> the validation runs within.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validator">The <see cref="IValidator"/> to run.</param>
    /// <returns>A <see cref="CommandResult"/> describing the validation outcome.</returns>
    async Task<CommandResult> RunValidator(CommandContext context, object instance, IValidator validator)
    {
        var commandResult = CommandResult.Success(context.CorrelationId);

        try
        {
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(instance.GetType());
            var validationContext = Activator.CreateInstance(validationContextType, instance) as IValidationContext;
            var validationResult = await validator.ValidateAsync(validationContext, context.CancellationToken);
            if (!validationResult.IsValid)
            {
                commandResult.MergeWith(new CommandResult
                {
                    ValidationResults = validationResult.Errors.Select(_ =>
                    {
                        var severity = _.Severity switch
                        {
                            FluentValidation.Severity.Info => ValidationResultSeverity.Information,
                            FluentValidation.Severity.Warning => ValidationResultSeverity.Warning,
                            FluentValidation.Severity.Error => ValidationResultSeverity.Error,
                            _ => ValidationResultSeverity.Error
                        };
                        return new ValidationResult(severity, _.ErrorMessage, [_.PropertyName], _.CustomState ?? null!);
                    }).ToArray()
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A validator that dereferences a null concept member throws while validating hostile or partial
            // input. Surface it as a validation failure (HTTP 400) rather than letting it propagate to a server
            // error (HTTP 500). The detail is logged server-side and never returned to the client. Cancellation
            // is deliberately excluded so a cancelled request is not mistaken for invalid input.
            logger.CommandValidatorThrew(instance.GetType().FullName ?? instance.GetType().Name, ex);
            commandResult.MergeWith(new CommandResult
            {
                ValidationResults = [ValidationResult.Error("The command could not be validated.")]
            });
        }

        return commandResult;
    }

    /// <summary>
    /// Resolves a validator for the given model type, preferring the command-scoped <see cref="IServiceProvider"/>
    /// from the <see cref="CommandContext"/> so the validator and its dependencies resolve from the same scope as
    /// the command's <c>Handle()</c> method.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> the validation runs within.</param>
    /// <param name="modelType">The type to resolve a validator for.</param>
    /// <param name="validator">The resolved <see cref="IValidator"/> when found.</param>
    /// <returns>True if a validator was found; otherwise false.</returns>
    bool TryGetValidator(CommandContext context, Type modelType, [MaybeNullWhen(false)] out IValidator validator) =>
        context.ServiceProvider is { } serviceProvider
            ? discoverableValidators.TryGet(modelType, serviceProvider, out validator)
            : discoverableValidators.TryGet(modelType, out validator);
}
