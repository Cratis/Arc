// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>Rejects supplied foreign tokens and validators with unknown registration lifetimes.</summary>
internal sealed class DecisionDependencySafety : ICommandDependencySafety
{
    /// <inheritdoc/>
    public void ValidateProvided(object value) => CommandDecisionReads.VerifyProvided(value);

    /// <inheritdoc/>
    public void ValidateRegisteredValidator(Type validatorType)
    {
        // The validator registration's lifetime is not carried by IServiceProvider. A singleton could retain an
        // invocation's token for the next command, so do not guess at the lifetime of registered validators.
        if (validatorType.GetConstructors().Any(constructor => constructor.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(IDecisionReads) ||
                (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(DecisionRead<>)))))
        {
            throw new InvalidOperationException($"Registered validator '{validatorType}' cannot capture decision reads; use an unregistered per-invocation validator.");
        }
    }
}
