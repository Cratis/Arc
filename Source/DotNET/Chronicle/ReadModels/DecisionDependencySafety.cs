// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>Rejects supplied foreign tokens and validators with unknown registration lifetimes.</summary>
internal sealed class DecisionDependencySafety : ICommandDependencySafety, ICommandProtectedDecisionSupport
{
    /// <inheritdoc/>
    public void ValidateProvided(object value) => CommandDecisionReads.VerifyProvided(value);

    /// <inheritdoc/>
    public void ValidateRegisteredValidator(Type validatorType)
    {
        if (CommandDecisionPolicy.IsProtected)
        {
            throw new InvalidOperationException($"Registered validator '{validatorType}' cannot run in a protected decision command; use an unregistered per-invocation validator.");
        }
    }

    /// <inheritdoc/>
    public void EnsureSupported(IServiceProvider services)
    {
        if (services.GetService<IDecisionReads>() is not CommandDecisionReads)
        {
            throw new InvalidOperationException("Protected decisions require a command-aware Chronicle decision reader in the command provider.");
        }
    }

    /// <inheritdoc/>
    public void ValidateCommandDependency(Type dependencyType, object dependency)
    {
        if (dependencyType == typeof(IDecisionReads) && dependency is not CommandDecisionReads)
        {
            throw new InvalidOperationException("A protected command requires the command-aware IDecisionReads reader.");
        }

        if (dependencyType.IsGenericType && dependencyType.GetGenericTypeDefinition() == typeof(DecisionRead<>))
        {
            CommandDecisionReads.VerifyProvided(dependency);
        }
    }

    /// <inheritdoc/>
    public void ValidateValidatorDependency(Type dependencyType, object? dependency)
    {
        if (dependencyType == typeof(IDecisionReads))
        {
            if (dependency is not (null or CommandDecisionReads))
            {
                throw new InvalidOperationException("A protected validator requires the command-aware IDecisionReads reader.");
            }
            return;
        }

        if (dependencyType.IsGenericType && dependencyType.GetGenericTypeDefinition() == typeof(DecisionRead<>))
        {
            if (dependency is not null) CommandDecisionReads.VerifyProvided(dependency);
            return;
        }

        throw new InvalidOperationException($"Protected validator dependency '{dependencyType}' is unsupported; use a directly issued DecisionRead<T> or command-aware IDecisionReads.");
    }
}
