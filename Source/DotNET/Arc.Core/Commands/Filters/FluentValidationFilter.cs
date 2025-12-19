// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (discoverableValidators.TryGet(instanceType, out var validator))
        {
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(instance.GetType());
            var validationContext = Activator.CreateInstance(validationContextType, instance) as IValidationContext;
            var validationResult = await validator.ValidateAsync(validationContext, CancellationToken.None);
            if (!validationResult.IsValid)
            {
                commandResult.MergeWith(new CommandResult
                {
                    ValidationResults = validationResult.Errors.Select(_ =>
                        new ValidationResult(ValidationResultSeverity.Error, _.ErrorMessage, [_.PropertyName], null!)).ToArray()
                });
            }
        }

        if (!instanceType.IsPrimitive &&
            instanceType != typeof(string) &&
            instanceType != typeof(DateTime) &&
            instanceType != typeof(DateTimeOffset) &&
            instanceType != typeof(Guid) &&
            instanceType != typeof(decimal) &&
            !instanceType.IsArray &&
            !typeof(System.Collections.IEnumerable).IsAssignableFrom(instanceType))
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

        return commandResult;
    }
}
