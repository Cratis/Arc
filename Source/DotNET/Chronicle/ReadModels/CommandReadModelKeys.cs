// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Holds the command key properties and read model key types generated at compile time; intended for generated code only.
/// </summary>
/// <remarks>
/// Registration is idempotent: the first registration of a type wins, so a type reachable from several assemblies is harmless.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CommandReadModelKeys
{
    static readonly ConcurrentDictionary<Type, CommandReadModelKeyProperty[]> _commands = new();
    static readonly ConcurrentDictionary<Type, Type?> _models = new();
    static readonly ConcurrentDictionary<Type, byte> _warnedTypes = new();
    static readonly ConcurrentDictionary<(Type Type, bool PublicGettersOnly), CommandReadModelKeyProperty[]> _reflectionProperties = new();

    /// <summary>
    /// Registers the public readable instance properties of a command, in reflection order.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="properties">Every public readable non-indexed instance property, inherited ones included.</param>
    public static void RegisterCommand(Type command, params CommandReadModelKeyProperty[] properties) =>
        _commands.TryAdd(command, properties);

    /// <summary>
    /// Declares the type of a read model's <c>Id</c> property.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="keyType">The type of its <c>Id</c> property, or <see langword="null"/> when it has none.</param>
    public static void DeclareModelKey(Type model, Type? keyType) =>
        _models.TryAdd(model, keyType);

    /// <summary>
    /// Looks up the registered properties of a command.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="properties">The registered properties.</param>
    /// <returns>Whether the command is registered.</returns>
    internal static bool TryGetProperties(Type command, out CommandReadModelKeyProperty[] properties)
    {
        if (_commands.TryGetValue(command, out properties!))
        {
            return true;
        }

        RunModuleConstructor(command);
        return _commands.TryGetValue(command, out properties!);
    }

    /// <summary>
    /// Looks up the declared key type of a read model.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="keyType">The type of its <c>Id</c> property, or <see langword="null"/> when it has none.</param>
    /// <returns>Whether the model is declared.</returns>
    internal static bool TryGetKeyType(Type model, out Type? keyType)
    {
        if (_models.TryGetValue(model, out keyType))
        {
            return true;
        }

        RunModuleConstructor(model);
        return _models.TryGetValue(model, out keyType);
    }

    /// <summary>
    /// Gets registered command properties, or the shared reflection compatibility descriptors.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="configuration">The operational read-protection configuration.</param>
    /// <param name="logger">The optional fallback logger.</param>
    /// <returns>The command's readable properties.</returns>
    internal static CommandReadModelKeyProperty[] PropertiesFor(Type command, IConfiguration? configuration, ILogger? logger)
    {
        if (TryGetProperties(command, out var properties)) return properties;
        RequireMetadata(command, configuration, logger);
        return ReflectionProperties(command, publicGettersOnly: true);
    }

    /// <summary>
    /// Gets a registered model key type, or discovers it through the shared reflection compatibility path.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="configuration">The operational read-protection configuration.</param>
    /// <param name="logger">The optional fallback logger.</param>
    /// <returns>The model's key type, or null when it has no Id property.</returns>
    internal static Type? KeyTypeFor(Type model, IConfiguration? configuration, ILogger? logger)
    {
        if (TryGetKeyType(model, out var keyType)) return keyType;
        RequireMetadata(model, configuration, logger);
        return ReflectionProperties(model, publicGettersOnly: false).FirstOrDefault(property => property.Name == "Id")?.PropertyType;
    }

    /// <summary>
    /// Preflights a discovered command without reflecting over its properties.
    /// </summary>
    /// <param name="command">The discovered command type.</param>
    /// <param name="configuration">The operational read-protection configuration.</param>
    /// <param name="logger">The optional fallback logger.</param>
    internal static void EnsureCommandCoverage(Type command, IConfiguration? configuration, ILogger? logger)
    {
        if (!TryGetProperties(command, out _)) RequireMetadata(command, configuration, logger);
    }

    static CommandReadModelKeyProperty[] ReflectionProperties(Type type, bool publicGettersOnly) =>
        _reflectionProperties.GetOrAdd((type, publicGettersOnly), static key =>
            [.. ModelGraphValidator.ReadablePropertiesThroughReflection(key.Type, key.PublicGettersOnly)
                .Select(property => new CommandReadModelKeyProperty(property.Name, property.PropertyType, property.GetValue))]);

    static void RequireMetadata(Type type, IConfiguration? configuration, ILogger? logger)
    {
        if (ReadProtectionConfiguration.RequireGeneratedMetadata(configuration)) throw new CommandReadModelKeysMissing(type);
        if (logger?.IsEnabled(LogLevel.Warning) == true && _warnedTypes.TryAdd(type, 0))
        {
            logger.ReflectionFallback(type.FullName ?? type.Name);
        }
    }

    /// <summary>
    /// Runs the module initializer of a type's module; a module initializer only runs when its module is first touched, which merely holding a type does not do.
    /// </summary>
    /// <param name="type">The type whose module to initialize.</param>
    /// <remarks>
    /// The runtime serializes initialization and caches success or failure, so an in-progress run is never marked as complete here.
    /// </remarks>
    static void RunModuleConstructor(Type type) => RuntimeHelpers.RunModuleConstructor(type.Module.ModuleHandle);
}
