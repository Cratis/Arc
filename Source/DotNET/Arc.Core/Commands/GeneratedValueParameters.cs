// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands;

/// <summary>
/// Validates and caches generated value parameter declarations before command execution.
/// </summary>
internal static class GeneratedValueParameters
{
    static readonly ConcurrentDictionary<(Type Command, MethodInfo Handle, bool DynamicCode), ParameterInfo[]> _parameters = new();

    /// <summary>
    /// Describes a handler's parameters and rejects invalid generated declarations at startup.
    /// </summary>
    /// <param name="commandType">The command declaring the handler.</param>
    /// <param name="handleMethod">The Handle method.</param>
    /// <param name="dynamicCodeSupported">An optional dynamic-code capability override for specifications.</param>
    /// <returns>The validated parameter declarations.</returns>
    internal static ParameterInfo[] Describe(Type commandType, MethodInfo handleMethod, bool? dynamicCodeSupported = null) =>
        _parameters.GetOrAdd(
            (commandType, handleMethod, dynamicCodeSupported ?? RuntimeFeature.IsDynamicCodeSupported),
            static key => DescribeParameters(key.Command, key.Handle, key.DynamicCode));

    /// <summary>
    /// Determines whether a type implements the generatable interface for itself.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Whether the type is generatable.</returns>
    internal static bool IsGeneratable(Type type) => Implements(type, typeof(IGeneratable<>)) || IsGeneratableInContext(type);

    /// <summary>
    /// Determines whether a type implements the context-dependent generation contract for itself.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Whether the type is context-dependent.</returns>
    internal static bool IsGeneratableInContext(Type type) => Implements(type, typeof(IGeneratableInContext<>));

    /// <summary>
    /// Gets the explicit generated name or the parameter name.
    /// </summary>
    /// <param name="parameter">A generated parameter.</param>
    /// <returns>The generated value name.</returns>
    internal static string NameFor(ParameterInfo parameter) =>
        parameter.GetCustomAttribute<GeneratedValueAttribute>()!.Name ?? parameter.Name!;

    static bool Implements(Type type, Type contract) => type.GetInterfaces().Any(candidate =>
        candidate.IsGenericType && candidate.GetGenericTypeDefinition() == contract &&
        candidate.GenericTypeArguments[0] == type);

    static ParameterInfo[] DescribeParameters(Type commandType, MethodInfo handleMethod, bool dynamicCodeSupported)
    {
        RuntimeHelpers.RunModuleConstructor(commandType.Module.ModuleHandle);
        var provideParameters = commandType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.Name == "Provide")
            .SelectMany(method => method.GetParameters())
            .Where(parameter => parameter.IsDefined(typeof(GeneratedValueAttribute)));
        foreach (var parameter in provideParameters)
        {
            throw new InvalidGeneratedValueParameter(commandType, parameter.Name!, "only Handle parameters can be generated, not Provide parameters.");
        }

        var parameters = handleMethod.GetParameters();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in parameters.Where(parameter => parameter.IsDefined(typeof(GeneratedValueAttribute))))
        {
            ValidateParameter(commandType, parameter, names, dynamicCodeSupported);
        }

        return parameters;
    }

    static void ValidateParameter(Type commandType, ParameterInfo parameter, HashSet<string> names, bool dynamicCodeSupported)
    {
        var type = parameter.ParameterType;
        if (!IsGeneratable(type))
        {
            throw new InvalidGeneratedValueParameter(commandType, parameter.Name!, "the type must implement IGeneratable<TSelf> or IGeneratableInContext<TSelf> for itself.");
        }

        if (new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable)
        {
            throw new InvalidGeneratedValueParameter(commandType, parameter.Name!, "the type must be non-nullable.");
        }

        var name = NameFor(parameter);
        if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
        {
            throw new InvalidGeneratedValueParameter(commandType, parameter.Name!, "generated value names must be non-empty and unique within the command.");
        }

        if (!GeneratedValueFactories.HasFactory(type) && !dynamicCodeSupported)
        {
            throw new MissingGeneratedValueFactory(commandType, parameter.Name!, type);
        }
    }
}
