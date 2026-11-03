// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Describes, without reflection, how the configuration binder treats one declared type.
/// </summary>
/// <param name="clrType">The declared type, as the binder formats it in messages.</param>
internal abstract class BoundType(Type clrType)
{
    /// <summary>
    /// Gets the declared type.
    /// </summary>
    internal Type ClrType { get; } = clrType;

    /// <summary>
    /// Converts a configuration value when the type has a string converter.
    /// </summary>
    /// <param name="value">The configuration value, which is <see langword="null"/> for an explicit null.</param>
    /// <param name="path">The configuration path.</param>
    /// <param name="result">The converted value.</param>
    /// <param name="error">The binder error, if the conversion failed.</param>
    /// <returns><see langword="true"/> when the type is converted from a string, otherwise <see langword="false"/>.</returns>
    internal virtual bool TryConvert(string? value, string? path, out object? result, out Exception? error)
    {
        result = null;
        error = null;
        return false;
    }

    /// <summary>
    /// Creates an instance the way the binder's CreateInstance does.
    /// </summary>
    /// <returns>The new instance.</returns>
    internal abstract object CreateInstance();

    /// <summary>
    /// Binds a configuration section that has children.
    /// </summary>
    /// <param name="point">The binding point.</param>
    /// <param name="config">The configuration section.</param>
    internal abstract void BindChildren(BindingPoint point, IConfiguration config);
}
