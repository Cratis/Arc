// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Rejects supplied foreign tokens and validators with unknown registration lifetimes.
/// </summary>
internal sealed class DecisionDependencySafety : ICommandDependencySafety, ICommandProtectedDecisionSupport
{
    /// <inheritdoc/>
    public void ValidateProvided(object value) => CommandDecisionReads.VerifyProvided(value);

    /// <inheritdoc/>
    public void ValidateRegisteredValidator(Type validatorType)
    {
        if (CommandDecisionPolicy.IsProtected)
        {
            throw new RegisteredValidatorRefusedInProtectedDecision(validatorType);
        }
    }

    /// <inheritdoc/>
    public void EnsureSupported(IServiceProvider services)
    {
        if (services.GetService<IDecisionReads>() is not CommandDecisionReads)
        {
            throw new ProtectedDecisionsRequireCommandAwareReader();
        }
    }

    /// <inheritdoc/>
    public void ValidateCommandDependency(Type dependencyType, object? dependency)
    {
        if (dependencyType == typeof(IDecisionReads) && dependency is not CommandDecisionReads)
        {
            throw new ProtectedCommandRequiresCommandAwareReader();
        }

        if (IsDecisionRead(dependencyType))
        {
            if (dependency is null)
            {
                throw new ProtectedCommandRequiresIssuedDecisionRead();
            }
            CommandDecisionReads.VerifyProvided(dependency);
        }
    }

    /// <inheritdoc/>
    public void ValidateValidatorDependencyShape(Type dependencyType) =>
        throw new ProtectedValidatorDependencyUnsupported(dependencyType);

    /// <inheritdoc/>
    public void ValidateValidatorDependency(Type dependencyType, object? dependency) => ValidateValidatorDependencyShape(dependencyType);

    static bool IsDecisionRead(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>);
}
