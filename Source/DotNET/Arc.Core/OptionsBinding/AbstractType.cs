// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// An abstract class or interface. It has no string converter and can not be created.
/// </summary>
/// <remarks>
/// When an instance is already present the reflection binder walks the public properties of its runtime type. That type is
/// not known here, so the present instance goes through <see cref="RuntimeTypeFallback"/>.
/// </remarks>
internal sealed class AbstractType : BoundType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractType"/> class.
    /// </summary>
    /// <param name="clrType">The abstract type or interface.</param>
    internal AbstractType(Type clrType)
        : base(clrType)
    {
    }

    /// <inheritdoc/>
    internal override object CreateInstance() =>
        throw new InvalidOperationException(BinderMessages.CannotActivateAbstractOrInterface(ClrType));

    /// <inheritdoc/>
    internal override void BindChildren(BindingPoint point, IConfiguration config)
    {
        if (point.Value is null && !point.IsReadOnly)
        {
            point.SetValue(CreateInstance());
        }

        RuntimeTypeFallback.BindWhenSubtype(this, point, config);
    }
}
