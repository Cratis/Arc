// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands;

/// <summary>
/// Resolves, validates and captures a generated command argument without logging its value.
/// </summary>
internal sealed class GeneratedValueResolution
{
    /// <summary>
    /// Reuses async validation and severity mapping without logging validator exceptions, whose messages may contain values.
    /// </summary>
    static readonly ValidatorInvoker _validatorInvoker = new(NullLogger<ValidatorInvoker>.Instance);

    /// <summary>
    /// Resolves a generated argument, validates it and records its capture.
    /// </summary>
    /// <param name="commandType">The command declaring the argument.</param>
    /// <param name="parameter">The generated parameter.</param>
    /// <param name="context">The command execution context.</param>
    /// <param name="services">The scoped service provider.</param>
    /// <returns>The validated generated value.</returns>
    /// <exception cref="InvalidGeneratedValueParameter">The factory or fixture supplied an incompatible value.</exception>
    /// <exception cref="GeneratedValueRejected">The generated value failed validation.</exception>
    /// <exception cref="OperationCanceledException">Command execution was cancelled.</exception>
    internal static async Task<object> Resolve(Type commandType, ParameterInfo parameter, CommandContext context, IServiceProvider services)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var name = GeneratedValueParameters.NameFor(parameter);
        var type = parameter.ParameterType;
        var fixture = services.GetService<IGeneratedValueFixture>();
        var value = fixture is not null && fixture.TryGet(commandType, name, type, out var pinned)
            ? pinned
            : GeneratedValueFactories.Create(commandType, parameter);
        if (!type.IsInstanceOfType(value))
        {
            throw new InvalidGeneratedValueParameter(commandType, parameter.Name!, "the factory or fixture must supply a non-null value of the declared type.");
        }

        await Validate(commandType, name, type, value, services, context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        context.GeneratedValues.Add(name, type, value);
        fixture?.Record(commandType, context.GeneratedValues[^1]);
        return value;
    }

    static async Task Validate(Type commandType, string name, Type type, object value, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (services.GetService<IDiscoverableValidators>() is not { } validators || !validators.TryGet(type, services, out var validator))
        {
            return;
        }

        var failures = (await _validatorInvoker.Invoke(value, validator, name, cancellationToken)).ToArray();
        var errors = failures.Where(failure => failure.Severity is not (ValidationResultSeverity.Warning or ValidationResultSeverity.Information)).ToArray();
        if (errors.Length > 0)
        {
            throw new GeneratedValueRejected(commandType, name, type, errors.Select(failure => failure.Message).ToArray());
        }

        if (failures.Length > 0)
        {
            services.GetService<ILogger<GeneratedValueResolution>>()?.GeneratedValueWarning(commandType.FullName!, name, type.FullName!);
        }
    }
}
