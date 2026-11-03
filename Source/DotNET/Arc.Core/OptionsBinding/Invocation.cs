// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Calls accessors and reports their failures the way a property call through reflection does.
/// </summary>
internal static class Invocation
{
    /// <summary>
    /// Calls a getter. Any failure surfaces as a <see cref="TargetInvocationException"/>, as for <c>PropertyInfo.GetValue</c>.
    /// </summary>
    /// <typeparam name="TOwner">The owner type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="owner">The owner.</param>
    /// <param name="getter">The getter.</param>
    /// <returns>The value.</returns>
    /// <exception cref="TargetInvocationException">The getter failed.</exception>
    internal static object? Get<TOwner, TValue>(TOwner owner, Func<TOwner, TValue> getter)
    {
        try
        {
            return getter(owner);
        }
        catch (Exception ex)
        {
            throw new TargetInvocationException(ex);
        }
    }

    /// <summary>
    /// Calls a setter. Any failure surfaces as a <see cref="TargetInvocationException"/>, as for <c>PropertyInfo.SetValue</c>.
    /// </summary>
    /// <typeparam name="TOwner">The owner type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="owner">The owner.</param>
    /// <param name="setter">The setter.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="TargetInvocationException">The setter failed.</exception>
    internal static void Set<TOwner, TValue>(TOwner owner, Action<TOwner, TValue> setter, TValue value)
    {
        try
        {
            setter(owner, value);
        }
        catch (Exception ex)
        {
            throw new TargetInvocationException(ex);
        }
    }
}
