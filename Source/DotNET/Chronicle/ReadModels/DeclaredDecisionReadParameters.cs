// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Discovers parameter reads using either the declared or effective protection policy.
/// </summary>
internal static class DeclaredDecisionReadParameters
{
    const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Finds injected wrapped decision reads and, under On, plain model parameters and validator dependencies.
    /// </summary>
    /// <param name="command">The command to inspect.</param>
    /// <param name="readModels">The models recognized by Chronicle parameter resolution.</param>
    /// <param name="types">The types containing discoverable validators.</param>
    /// <param name="protection">Whether to use the declared policy, and the services supplying the runtime cap.</param>
    /// <returns>The distinct models and whether each parameter supplies stream scope.</returns>
    internal static IEnumerable<(Type Model, bool StreamScoped)> ReadsOf(Type command, IReadOnlySet<Type> readModels, ITypes types, (bool Declared, IServiceProvider? Services) protection = default)
    {
        using var policy = CommandDecisionPolicy.Begin(command, protection.Services);
        ValidateChains(command, readModels, types, protection.Services);
        if (!CommandDecisionPolicy.IsProtected)
        {
            return [];
        }

        var on = (protection.Declared ? CommandDecisionPolicy.DeclaredReadProtection : CommandDecisionPolicy.ReadProtection) == ReadProtectionMode.On;
        return BatchesOf(command, types, on).SelectMany(parameters => parameters)
            .Select(parameter => ReadOf(parameter, on, readModels))
            .OfType<(Type Model, bool StreamScoped)>()
            .Where(_ => !_.Model.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    }

    /// <summary>Preflights each member independently, including when an operational cap disables protection.</summary>
    /// <param name="command">The discovered command.</param>
    /// <param name="readModels">The registered read-model types.</param>
    /// <param name="types">The discovered application types.</param>
    /// <param name="services">The services supplying the operational cap.</param>
    internal static void ValidateChains(Type command, IReadOnlySet<Type> readModels, ITypes types, IServiceProvider? services)
    {
        using var policy = CommandDecisionPolicy.Begin(command, services);
        foreach (var parameters in BatchesOf(command, types, includeValidators: true))
        {
            ChainedReadModelLookup.Order(command, parameters, readModels, CommandDecisionPolicy.ReadProtection);
        }
    }

    static IEnumerable<ParameterInfo[]> BatchesOf(Type command, ITypes types, bool includeValidators)
    {
        var provide = command.GetProvideMethod();
        var batches = command.GetMethods(Methods).Where(method => method.Name == "Handle")
            .Select(method => InjectedHandleParameters.Of(method, provide).ToArray())
            .Concat(provide is null ? [] : new[] { provide.GetParameters() });
        return includeValidators ? batches.Concat(ValidatorConstructors(command, types).Select(constructor => constructor.GetParameters())) : batches;
    }

    static IEnumerable<ConstructorInfo> ValidatorConstructors(Type command, ITypes types) =>
        types.FindMultiple(typeof(IDiscoverableValidator<>))
            .Where(_ => _.GetInterfaces().Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>) &&
                contract.GetGenericArguments()[0] == command))
            .Select(_ => _.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length).FirstOrDefault())
            .OfType<ConstructorInfo>();

    static (Type Model, bool StreamScoped)? ReadOf(ParameterInfo parameter, bool on, IReadOnlySet<Type> readModels)
    {
        var type = parameter.ParameterType;
        var wrapped = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type.GetGenericArguments()[0] : type;
        if (wrapped.IsGenericType && wrapped.GetGenericTypeDefinition() == typeof(DecisionRead<>))
        {
            var model = wrapped.GetGenericArguments()[0];
            return (model, PerStreamReadModelBinding.IsDeclared(model));
        }

        return on && readModels.Contains(type) &&
            !Attribute.IsDefined(parameter, typeof(UnprotectedAttribute)) && !Attribute.IsDefined(type, typeof(UnprotectedAttribute))
                ? (type, PerStreamReadModelBinding.IsDeclared(type)) : null;
    }
}
