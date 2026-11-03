// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Binds a runtime subtype instead of using its declared type's binding table.
/// </summary>
/// <remarks>
/// The reflection binder walks the runtime type of a value. The binding tables describe the declared types, so a value whose
/// type is a subclass of the declared type must be bound as a whole by the stock binder to preserve hidden members and
/// runtime property order. The declared table is skipped, so collections are not bound twice. The original configuration
/// is passed through unchanged to preserve section paths and distinguish absent values from explicit nulls.
/// Arc's own types never take this path.
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
    /// Binds the value when it is a subtype of the declared type, replacing declared-table binding.
    /// </summary>
    /// <param name="declared">The declared type.</param>
    /// <param name="point">The binding point holding the value.</param>
    /// <param name="config">The configuration section of the value.</param>
    /// <returns>Whether the runtime subtype was bound.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only reached for a value whose runtime type differs from the declared type, which means the application supplied a subtype. Arc's own types never take this path.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only reached for a value whose runtime type differs from the declared type, which means the application supplied a subtype. Arc's own types never take this path.")]
    internal static bool BindWhenSubtype(BoundType declared, BindingPoint point, IConfiguration config)
    {
        if (point.Value is not { } value || value.GetType() == declared.ClrType)
        {
            return false;
        }

        WarnWhenDynamicCodeIsUnavailable(_logger, value.GetType(), RuntimeFeature.IsDynamicCodeSupported);
        _bindCount++;
        Bind(value, config);
        return true;
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
}
