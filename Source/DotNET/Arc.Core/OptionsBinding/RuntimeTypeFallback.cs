// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Binds the members of a runtime subtype that the binding tables do not describe.
/// </summary>
/// <remarks>
/// The reflection binder walks the runtime type of a value. The binding tables describe the declared types, so a value whose
/// type is a subclass of the declared type (for instance a subclass of an option type that adds its own properties) would
/// lose the keys of those properties. For exactly that case the stock binder binds the keys that the declared type does not
/// own. Arc's own types never take this path.
/// </remarks>
internal static class RuntimeTypeFallback
{
    static readonly ConcurrentDictionary<Type, bool> _warnedTypes = new();

    [ThreadStatic]
    static ILogger? _logger;

    [ThreadStatic]
    static int _bindCount;

    /// <summary>
    /// Gets the number of fallback binds that ran on the current thread since <see cref="ResetBindCount"/>.
    /// </summary>
    internal static int BindCount => _bindCount;

    /// <summary>
    /// Sets the logger that receives the warning for the bind running on the current thread.
    /// </summary>
    /// <param name="logger">The logger, if any.</param>
    internal static void UseLogger(ILogger? logger) => _logger = logger;

    /// <summary>
    /// Resets <see cref="BindCount"/> for the current thread.
    /// </summary>
    internal static void ResetBindCount() => _bindCount = 0;

    /// <summary>
    /// Binds the keys that the declared type does not own when the value is of a subtype of it.
    /// </summary>
    /// <param name="declared">The declared type.</param>
    /// <param name="declaredNames">The names of the members the declared type binds itself.</param>
    /// <param name="point">The binding point holding the value.</param>
    /// <param name="config">The configuration section of the value.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only reached for a value whose runtime type differs from the declared type, which means the application supplied a subtype. Arc's own types never take this path.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only reached for a value whose runtime type differs from the declared type, which means the application supplied a subtype. Arc's own types never take this path.")]
    internal static void BindWhenSubtype(BoundType declared, IReadOnlyCollection<string> declaredNames, BindingPoint point, IConfiguration config)
    {
        if (point.Value is not { } value || value.GetType() == declared.ClrType)
        {
            return;
        }

        var remaining = WithoutDeclaredKeys(config, declaredNames);
        if (!remaining.GetChildren().Any())
        {
            return;
        }

        WarnWhenDynamicCodeIsUnavailable(_logger, value.GetType(), RuntimeFeature.IsDynamicCodeSupported);
        _bindCount++;
        Bind(value, remaining);
    }

    /// <summary>
    /// Logs, once per type, that a subtype is bound through the stock binder without dynamic code support.
    /// </summary>
    /// <param name="logger">The logger, if any.</param>
    /// <param name="type">The runtime type.</param>
    /// <param name="isDynamicCodeSupported">Whether dynamic code is supported.</param>
    internal static void WarnWhenDynamicCodeIsUnavailable(ILogger? logger, Type type, bool isDynamicCodeSupported)
    {
        if (isDynamicCodeSupported || logger is null || !_warnedTypes.TryAdd(type, true))
        {
            return;
        }

        logger.RuntimeSubtypeBoundWithoutDynamicCode(type.FullName ?? type.Name);
    }

    [RequiresUnreferencedCode("Binds members of a runtime subtype that the trimmer can not see.")]
    [RequiresDynamicCode("The stock binder instantiates generic types.")]
    static void Bind(object instance, IConfiguration config) => config.Bind(instance);

    static IConfiguration WithoutDeclaredKeys(IConfiguration config, IReadOnlyCollection<string> declaredNames)
    {
        var values = new Dictionary<string, string?>();
        var prefixLength = config is IConfigurationSection section ? section.Path.Length + 1 : 0;
        foreach (var child in config.GetChildren().Where(_ => !declaredNames.Contains(_.Key, StringComparer.OrdinalIgnoreCase)))
        {
            foreach (var pair in child.AsEnumerable(false))
            {
                values[pair.Key[prefixLength..]] = pair.Value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
