// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Caches artifact-derived command dependencies within each type universe.
/// </summary>
internal static class CommandDecisionDependencies
{
    static readonly ConditionalWeakTable<ITypes, Cache> _caches = new();

    /// <summary>
    /// Checks whether a command declares a protected decision dependency.
    /// </summary>
    /// <param name="commandType">The command type to inspect.</param>
    /// <returns>Whether the command or one of its validators declares a decision dependency.</returns>
    internal static bool Declares(Type commandType) => ForCurrentUniverse().Declares(commandType);

    /// <summary>
    /// Gets the discovered execution scope types, excluding scenario-owned scopes.
    /// </summary>
    /// <returns>The execution scope types for the current universe.</returns>
    internal static Type[] ExecutionScopeTypes() => ForCurrentUniverse().ExecutionScopeTypes;

    static Cache ForCurrentUniverse() => _caches.GetValue(TypesServiceCollectionExtensions.CurrentTypeUniverse(), types => new Cache(types));

    static bool IsDecisionDependency(Type type) =>
        type == typeof(IDecisionReads) || type == typeof(IStreamReads) || type == typeof(StreamDecision) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DecisionRead<>)) ||
        PerStreamReadModelBinding.IsDeclared(type);

    sealed class Cache(ITypes types)
    {
        readonly Lazy<Type[]> _validatorTypes = new(() => types.FindMultiple(typeof(IDiscoverableValidator<>)).ToArray());
        readonly Lazy<Type[]> _executionScopeTypes = new(() => types.FindMultiple<ICommandExecutionScope>()
            .Where(type => type != typeof(DecisionScenarioConcurrentAppendScope) && type != typeof(DecisionScenarioCommandCaptureScope))
            .ToArray());
        readonly ConcurrentDictionary<Type, bool> _declarations = new();

        internal Type[] ExecutionScopeTypes => _executionScopeTypes.Value;

        internal bool Declares(Type commandType) => _declarations.GetOrAdd(commandType, Compute);

        bool Compute(Type commandType)
        {
            var dependencies = commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(_ => _.Name == "Handle" || _.Name == "Provide")
                .SelectMany(_ => _.GetParameters());
            var validators = _validatorTypes.Value
                .Where(_ => typeof(IDiscoverableValidator<>).MakeGenericType(commandType).IsAssignableFrom(_));
            return dependencies.Concat(validators.SelectMany(_ => _.GetConstructors()).SelectMany(_ => _.GetParameters()))
                .Any(_ => IsDecisionDependency(_.ParameterType) ||
                    _.GetCustomAttribute<GeneratedValueAttribute>()?.Freshness == GeneratedValueFreshness.Guarded);
        }
    }
}
