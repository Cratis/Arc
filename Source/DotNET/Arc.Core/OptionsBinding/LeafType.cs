// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// A type the binder converts from a string through its type converter, such as a number, an enum, a string or a
/// <see cref="TimeSpan"/>.
/// </summary>
internal sealed class LeafType : BoundType
{
    readonly TypeConverter _converter;
    readonly Func<object>? _createDefault;
    readonly Func<Member[]>? _members;
    Member[]? _resolvedMembers;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeafType"/> class.
    /// </summary>
    /// <param name="clrType">The type.</param>
    /// <param name="converter">The converter <c>TypeDescriptor.GetConverter</c> yields for the type.</param>
    /// <param name="createDefault">Creates the default value of a value type, or <see langword="null"/> for a reference type.</param>
    /// <param name="members">The public instance properties the binder walks when a value has children.</param>
    internal LeafType(Type clrType, TypeConverter converter, Func<object>? createDefault, Func<Member[]>? members = null)
        : base(clrType)
    {
        _converter = converter;
        _createDefault = createDefault;
        _members = members;
    }

    /// <inheritdoc/>
    internal override bool TryConvert(string? value, string? path, out object? result, out Exception? error)
    {
        error = null;
        result = null;
        try
        {
            if (value is not null)
            {
                result = _converter.ConvertFromInvariantString(value);
            }
        }
        catch (Exception ex)
        {
            // ConfigurationBinder.TryConvertValue: any converter failure becomes the binder's own error.
            error = new InvalidOperationException(BinderMessages.FailedBinding(value, path, ClrType), ex);
        }

        return true;
    }

    /// <inheritdoc/>
    internal override object CreateInstance() =>
        _createDefault?.Invoke() ?? throw new InvalidOperationException(BinderMessages.MultipleParameterizedConstructors(ClrType));

    /// <inheritdoc/>
    internal override void BindChildren(BindingPoint point, IConfiguration config)
    {
        if (point.Value is null)
        {
            if (point.IsReadOnly)
            {
                return;
            }

            point.SetValue(CreateInstance());
        }

        if (RuntimeTypeFallback.BindWhenSubtype(this, point, config))
        {
            return;
        }

        if (_members is null)
        {
            return;
        }

        _resolvedMembers ??= _members();
        foreach (var member in _resolvedMembers)
        {
            member.Bind(point.Value!, config);
        }
    }
}
