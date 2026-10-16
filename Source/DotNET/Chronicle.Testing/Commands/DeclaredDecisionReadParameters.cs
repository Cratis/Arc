// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Discovers parameter reads using the declared policy, without the operational runtime cap.
/// </summary>
internal static class DeclaredDecisionReadParameters
{
    const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Finds wrapped decision reads and, under On, plain model parameters and validator dependencies.
    /// </summary>
    /// <param name="command">The command to inspect.</param>
    /// <param name="readModels">The models recognized by Chronicle parameter resolution.</param>
    /// <returns>The distinct models and whether each parameter supplies stream scope.</returns>
    internal static IEnumerable<(Type Model, bool StreamScoped)> ReadsOf(Type command, IReadOnlySet<Type> readModels)
    {
        using var policy = CommandDecisionPolicy.Begin(command);
        if (!CommandDecisionPolicy.IsProtected)
        {
            return [];
        }

        var on = CommandDecisionPolicy.DeclaredReadProtection == ReadProtectionMode.On;
        var parameters = command.GetMethods(Methods)
            .Where(_ => _.Name == "Handle" || _.Name == CommandProvideMethodExtensions.ProvideMethodName)
            .SelectMany(_ => _.GetParameters());
        if (on)
        {
            parameters = parameters.Concat(ValidatorParameters(command));
        }

        return parameters.Select(parameter => ReadOf(parameter, on, readModels))
            .OfType<(Type Model, bool StreamScoped)>()
            .Where(_ => !_.Model.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    }

    static IEnumerable<ParameterInfo> ValidatorParameters(Type command) =>
        TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple(typeof(IDiscoverableValidator<>))
            .Where(_ => typeof(IDiscoverableValidator<>).MakeGenericType(command).IsAssignableFrom(_))
            .Select(_ => _.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length).FirstOrDefault())
            .OfType<ConstructorInfo>()
            .SelectMany(_ => _.GetParameters());

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
