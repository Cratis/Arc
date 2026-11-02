// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// An <c>IList&lt;T&gt;</c> that is appended to from the child keys of the section.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class ListType<T> : BoundType
{
    readonly BoundType _element;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListType{T}"/> class.
    /// </summary>
    /// <param name="element">How the element type is bound.</param>
    internal ListType(BoundType element)
        : base(typeof(IList<T>))
    {
        _element = element;
    }

    /// <inheritdoc/>
    internal override object CreateInstance() =>
        throw new InvalidOperationException(BinderMessages.CannotActivateAbstractOrInterface(ClrType));

    /// <inheritdoc/>
    internal override void BindChildren(BindingPoint point, IConfiguration config)
    {
        if (point.Value is null)
        {
            if (point.IsReadOnly)
            {
                return;
            }

            point.SetValue(new List<T>());
        }

        if (point.Value is not ICollection<T> collection)
        {
            return;
        }

        foreach (var child in config.GetChildren())
        {
            try
            {
                var elementPoint = new BindingPoint();
                BinderSemantics.BindInstance(_element, elementPoint, child, true);
                if (elementPoint.HasNewValue)
                {
                    collection.Add(elementPoint.Value is null ? default! : (T)elementPoint.Value);
                }
            }
            catch (Exception ex) when (BinderSemantics.IgnoresElementFailure(ex))
            {
                // ConfigurationBinder.BindCollection ignores a failing element and continues with the next one.
            }
        }
    }
}
