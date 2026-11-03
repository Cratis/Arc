// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// The exception that is thrown when a validator's rules cannot be extracted.
/// </summary>
/// <param name="validatedType">The type being validated.</param>
/// <param name="validatorType">The validator whose rules could not be extracted.</param>
/// <param name="inner">The extraction failure.</param>
internal sealed class ValidationRulesCouldNotBeExtracted(Type validatedType, Type validatorType, Exception inner)
    : Exception($"Could not extract validation rules for '{validatedType.FullName}' from validator '{validatorType.FullName}': {RootCause(inner).GetType().Name}: {RootCause(inner).Message}", RootCause(inner))
{
    static Exception RootCause(Exception exception)
    {
        while (exception is TargetInvocationException or TypeInitializationException && exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }
}
