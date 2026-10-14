// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Resolves Chronicle-backed parameters independently of type-based scoped registrations.
/// </summary>
/// <param name="readModelTypes">The models backed by Chronicle artifacts.</param>
internal sealed class ChronicleParameterDependencyResolver(IEnumerable<Type> readModelTypes) : ICanResolveParameterDependency
{
    readonly HashSet<Type> _readModelTypes = readModelTypes.ToHashSet();

    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        dependency = null;
        if (!_readModelTypes.Contains(parameter.ParameterType) ||
            (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.Off &&
             !Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().Any()))
        {
            return false;
        }

        var context = serviceProvider.GetRequiredService<ICommandContextAccessor>().Current;
        var key = CommandReadModelLookup.Resolve(context, parameter, CommandDecisionPolicy.ReadProtection);
        dependency = ReadModelServiceCollectionExtensions.ResolveReadModel(parameter.ParameterType, context, serviceProvider.GetRequiredService<IReadModels>(), key);

        return true;
    }
}
