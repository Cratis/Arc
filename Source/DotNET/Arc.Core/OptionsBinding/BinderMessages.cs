// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The messages of the Microsoft configuration binder, formatted exactly as the binder of the same major version does:
/// 10 on net10.0 and 9 on net8.0 and net9.0.
/// </summary>
internal static class BinderMessages
{
    const string UseSystemResourceKeys = "System.Resources.UseSystemResourceKeys";

    /// <summary>
    /// Formats the message for a value that can not be converted.
    /// </summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="path">The configuration path.</param>
    /// <param name="type">The target type.</param>
    /// <returns>The message.</returns>
    internal static string FailedBinding(string? value, string? path, Type type) =>
#if NET10_0_OR_GREATER
        Format("Error_FailedBinding", "Failed to convert configuration value '{0}' at '{1}' to type '{2}'.", value, path, type);
#else
        Format("Error_FailedBinding", "Failed to convert configuration value at '{0}' to type '{1}'.", path, type);
#endif

    /// <summary>
    /// Formats the message for an abstract type or interface that can not be created.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The message.</returns>
    internal static string CannotActivateAbstractOrInterface(Type type) =>
        Format("Error_CannotActivateAbstractOrInterface", "Cannot create instance of type '{0}' because it is either abstract or an interface.", type);

    /// <summary>
    /// Formats the message for a type with several parameterized constructors and no parameterless one.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The message.</returns>
    internal static string MultipleParameterizedConstructors(Type type) =>
        Format("Error_MultipleParameterizedConstructors", "Cannot create instance of type '{0}' because it has multiple public parameterized constructors.", type);

    static string Format(string resourceKey, string resourceFormat, params object?[] args)
    {
        if (AppContext.TryGetSwitch(UseSystemResourceKeys, out var useKeys) && useKeys)
        {
            return resourceKey + ", " + string.Join(", ", args);
        }

        return string.Format(resourceFormat, args);
    }
}
