// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that validates commands before they are handled.
/// </summary>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for finding validators.</param>
public class FluentValidationFilter(IDiscoverableValidators discoverableValidators) : ICommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var commandResult = CommandResult.Success(context.CorrelationId);
        commandResult.MergeWith(await Validate(context, context.Command));
        return commandResult;
    }

    async Task<CommandResult> Validate(CommandContext context, object instance)
    {
        var commandResult = CommandResult.Success(context.CorrelationId);

        var instanceType = instance.GetType();
        if (TryGetValidator(context, instanceType, out var validator))
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
                    commandResult.MergeWith(await Validate(context, element));
                }
            }
            else
            {
                foreach (var property in instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var propertyValue = property.GetValue(instance);
                    if (propertyValue is not null)
                    {
                        commandResult.MergeWith(await Validate(context, propertyValue));
                    }
                }
            }
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
