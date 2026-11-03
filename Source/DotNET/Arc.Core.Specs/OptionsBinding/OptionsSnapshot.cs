// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Writes every readable public property of an object graph as text, so two bound instances can be compared exactly.
/// </summary>
/// <remarks>
/// This uses reflection on purpose. It is the parity oracle and runs untrimmed.
/// </remarks>
public static class OptionsSnapshot
{
    const int MaximumDepth = 8;

    /// <summary>
    /// Takes the snapshot.
    /// </summary>
    /// <param name="value">The object graph root.</param>
    /// <returns>The text of the graph.</returns>
    public static string Of(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value, 0, "$");
        return builder.ToString();
    }

    static void Write(StringBuilder builder, object? value, int depth, string path)
    {
        if (value is null)
        {
            builder.Append(path).AppendLine("=<null>");
            return;
        }

        var type = value.GetType();
        if (IsLeaf(type) || depth >= MaximumDepth)
        {
            builder.Append(path).Append('=').Append(type.Name).Append(':').AppendLine(value is Type t ? t.FullName : value.ToString());
            return;
        }

        if (value is IDictionary dictionary)
        {
            builder.Append(path).Append(".Count=").Append(dictionary.Count).AppendLine();
            foreach (DictionaryEntry entry in dictionary)
            {
                Write(builder, entry.Value, depth + 1, $"{path}[{entry.Key}]");
            }

            return;
        }

        if (value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                Write(builder, item, depth + 1, $"{path}[{index++}]");
            }

            builder.Append(path).Append(".Count=").Append(index).AppendLine();
            return;
        }

        builder.Append(path).Append(" is ").AppendLine(type.FullName);
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod is null || property.GetMethod.GetParameters().Length != 0)
            {
                continue;
            }

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch (TargetInvocationException ex)
            {
                builder.Append(path).Append('.').Append(property.Name).Append(" throws ").AppendLine(ex.InnerException?.GetType().Name);
                continue;
            }

            Write(builder, propertyValue, depth + 1, $"{path}.{property.Name}");
        }
    }

    static bool IsLeaf(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(TimeSpan) || type == typeof(Uri) || typeof(Type).IsAssignableFrom(type);
}
