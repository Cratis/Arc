// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Rejects supplied foreign decision tokens and providers without the command-aware reader.
/// </summary>
internal sealed class DecisionDependencySafety : ICommandDependencySafety, ICommandProtectedDecisionSupport
{
    /// <inheritdoc/>
    public void ValidateProvided(object value) => CommandDecisionReads.VerifyProvided(value);

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

    static bool IsDecisionRead(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>);
}
