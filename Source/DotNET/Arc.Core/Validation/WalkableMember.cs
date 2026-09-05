// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Cratis.Strings;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents a member of a model that a validation traversal descends into, and how to read it.
/// </summary>
/// <param name="Name">The member name, in the casing the client uses.</param>
/// <param name="Read">Reads the member's value from an instance of the declaring type.</param>
internal sealed record WalkableMember(string Name, Func<object, object?> Read)
{
    /// <summary>
    /// Creates a <see cref="WalkableMember"/> for a property.
    /// </summary>
    /// <param name="property">The <see cref="PropertyInfo"/> to describe.</param>
    /// <returns>The <see cref="WalkableMember"/>.</returns>
    /// <remarks>
    /// The reader is compiled once per property rather than going through <see cref="PropertyInfo.GetValue(object)"/>
    /// on every node of every traversal. Queries validate on every request, so the reflection cost is paid per
    /// request where a compiled accessor pays it once per property for the lifetime of the process. Compilation is
    /// best-effort: anything the expression compiler cannot express falls back to plain reflection, which is correct
    /// either way.
    /// </remarks>
    public static WalkableMember For(PropertyInfo property) =>
        new(property.Name.ToCamelCase(), CompileReader(property));

    static Func<object, object?> CompileReader(PropertyInfo property)
    {
        try
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var typed = Expression.Convert(instance, property.DeclaringType!);
            var read = Expression.Convert(Expression.Property(typed, property), typeof(object));
            return Expression.Lambda<Func<object, object?>>(read, instance).Compile();
        }
        catch (Exception)
        {
            return property.GetValue;
        }
    }
}
