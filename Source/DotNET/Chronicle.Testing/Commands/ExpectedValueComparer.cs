// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Compares expected event values through public readable properties and ordered collections.
/// </summary>
internal static class ExpectedValueComparer
{
    static readonly HashSet<Type> _scalarTypes =
    [
        typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset),
        typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(Uri)
    ];

    /// <summary>
    /// Finds the first difference between two event values.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="path">The path containing these values.</param>
    /// <returns>The first difference, or null when the values match.</returns>
    public static Difference? Compare(object? expected, object? actual, string path)
    {
        if (ReferenceEquals(expected, actual))
        {
            return null;
        }
        if (expected is null || actual is null)
        {
            return new(path, expected, actual);
        }
        if (expected is IEnumerable expectedItems and not string && actual is IEnumerable actualItems and not string)
        {
            return CompareItems(expectedItems.Cast<object?>().ToArray(), actualItems.Cast<object?>().ToArray(), path);
        }
        var type = expected.GetType();
        if (type != actual.GetType())
        {
            return new($"{path}.Type", type.FullName, actual.GetType().FullName);
        }
        if (IsScalar(type))
        {
            return Equals(expected, actual) ? null : new(path, expected, actual);
        }

        return CompareProperties(expected, actual, path);
    }

    static bool IsScalar(Type type) => type.IsPrimitive || type.IsEnum || _scalarTypes.Contains(type);

    static Difference? CompareProperties(object expected, object actual, string path)
    {
        var properties = expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.GetMethod?.IsPublic == true && _.GetIndexParameters().Length == 0).ToArray();
        if (properties.Length == 0)
        {
            return Equals(expected, actual) ? null : new(path, expected, actual);
        }
        foreach (var property in properties)
        {
            var difference = Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}");
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    static Difference? CompareItems(object?[] expected, object?[] actual, string path)
    {
        for (var index = 0; index < Math.Max(expected.Length, actual.Length); index++)
        {
            var itemPath = $"{path}[{index}]";
            if (index >= expected.Length)
            {
                return new(itemPath, "<missing>", actual[index]);
            }
            if (index >= actual.Length)
            {
                return new(itemPath, expected[index], "<missing>");
            }
            var difference = Compare(expected[index], actual[index], itemPath);
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    /// <summary>
    /// Describes the first differing member or collection element.
    /// </summary>
    /// <param name="Path">The member path.</param>
    /// <param name="Expected">The expected value.</param>
    /// <param name="Actual">The actual value.</param>
    internal sealed record Difference(string Path, object? Expected, object? Actual)
    {
        /// <inheritdoc/>
        public override string ToString() => $"{Path} expected '{Expected ?? "<null>"}' but was '{Actual ?? "<null>"}'";
    }
}
