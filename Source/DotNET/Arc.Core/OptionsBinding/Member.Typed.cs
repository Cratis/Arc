// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// A <see cref="Member"/> bound through typed accessors.
/// </summary>
/// <typeparam name="TOwner">The type that owns the property.</typeparam>
/// <typeparam name="TValue">The declared property type.</typeparam>
internal sealed class Member<TOwner, TValue> : Member
{
    readonly BoundType _type;
    readonly Func<TOwner, TValue> _getter;
    readonly Action<TOwner, TValue>? _setter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Member{TOwner, TValue}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="type">How the declared type is bound.</param>
    /// <param name="getter">The public getter.</param>
    /// <param name="setter">The public setter, or <see langword="null"/> for a get-only property.</param>
    internal Member(string name, BoundType type, Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter)
        : base(name)
    {
        _type = type;
        _getter = getter;
        _setter = setter;
    }

    /// <inheritdoc/>
    internal override void Bind(object owner, IConfiguration config)
    {
        var typedOwner = (TOwner)owner;
        var point = new BindingPoint(() => Invocation.Get(typedOwner, _getter), _setter is null);
        BinderSemantics.BindInstance(_type, point, config.GetSection(Name), false);

        // ConfigurationBinder.BindProperty: the value is assigned back when it is present or newly produced, which is what
        // makes a setter with side effects observable even when no configuration key addressed the member.
        var assign = !point.IsReadOnly && (point.Value is not null || point.HasNewValue);
        if (assign)
        {
            // A null assigned through a property call on a non-nullable value type stores its default, so do the same.
            Invocation.Set(typedOwner, _setter!, point.Value is null ? default! : (TValue)point.Value);
        }
    }
}
