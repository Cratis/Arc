// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// One bindable property of a type, in the order the reflection binder visits it.
/// </summary>
/// <param name="name">The configuration key, which is the property name.</param>
internal abstract class Member(string name)
{
    /// <summary>
    /// Gets the configuration key.
    /// </summary>
    internal string Name { get; } = name;

    /// <summary>
    /// Creates a member for a property.
    /// </summary>
    /// <typeparam name="TOwner">The type that owns the property.</typeparam>
    /// <typeparam name="TValue">The declared property type.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="type">How the declared type is bound.</param>
    /// <param name="getter">The public getter.</param>
    /// <param name="setter">The public setter, or <see langword="null"/> for a get-only property.</param>
    /// <returns>The member.</returns>
    internal static Member Of<TOwner, TValue>(string name, BoundType type, Func<TOwner, TValue> getter, Action<TOwner, TValue>? setter = null) =>
        new Member<TOwner, TValue>(name, type, getter, setter);

    /// <summary>
    /// Binds the member of an owner, as the binder's BindProperty does.
    /// </summary>
    /// <param name="owner">The owner instance.</param>
    /// <param name="config">The configuration section of the owner.</param>
    internal abstract void Bind(object owner, IConfiguration config);
}
