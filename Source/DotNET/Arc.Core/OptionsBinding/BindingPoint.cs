// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Mirrors the binding point of the Microsoft configuration binder: the current value of a member or element, whether it
/// is read-only and whether the bind produced a value that has to be assigned back.
/// </summary>
internal sealed class BindingPoint
{
    readonly Func<object?>? _initialValueProvider;
    object? _initialValue;
    object? _setValue;
    bool _valueSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingPoint"/> class with a known initial value.
    /// </summary>
    /// <param name="initialValue">The initial value, if any.</param>
    /// <param name="isReadOnly">Whether the point can not be assigned.</param>
    internal BindingPoint(object? initialValue = null, bool isReadOnly = false)
    {
        _initialValue = initialValue;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingPoint"/> class with a lazily read initial value.
    /// </summary>
    /// <param name="initialValueProvider">Reads the current value, for instance through a property getter.</param>
    /// <param name="isReadOnly">Whether the point can not be assigned.</param>
    internal BindingPoint(Func<object?> initialValueProvider, bool isReadOnly)
    {
        _initialValueProvider = initialValueProvider;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets a value indicating whether the point can not be assigned.
    /// </summary>
    internal bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether a value has been set, or the initial value is a non-primitive value type.
    /// </summary>
    internal bool HasNewValue
    {
        get
        {
            if (IsReadOnly)
            {
                return false;
            }

            if (_valueSet)
            {
                return true;
            }

            return _initialValue?.GetType() is { IsValueType: true, IsPrimitive: false };
        }
    }

    /// <summary>
    /// Gets the current value. The provider is invoked again for as long as it yields <see langword="null"/>.
    /// </summary>
    internal object? Value
    {
        get
        {
            if (_valueSet)
            {
                return _setValue;
            }

            return _initialValue ??= _initialValueProvider?.Invoke();
        }
    }

    /// <summary>
    /// Sets the value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    internal void SetValue(object? newValue)
    {
        _setValue = newValue;
        _valueSet = true;
    }

    /// <summary>
    /// Sets the value unless the point is read-only.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    internal void TrySetValue(object? newValue)
    {
        if (!IsReadOnly)
        {
            SetValue(newValue);
        }
    }
}
