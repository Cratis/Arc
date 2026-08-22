// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries;

/// <summary>
/// Captures an immutable serialized baseline of query arguments and creates independent deep clones from it.
/// </summary>
/// <param name="arguments">The arguments to capture.</param>
/// <param name="serializerOptions">The Arc JSON serializer options.</param>
internal sealed class ObservableQueryArgumentsSnapshot(
    QueryArguments arguments,
    JsonSerializerOptions serializerOptions)
{
    readonly Entry[] _entries = [.. arguments.Select(_ => Entry.Capture(_.Key, _.Value, serializerOptions))];
    readonly JsonSerializerOptions _serializerOptions = serializerOptions;

    /// <summary>
    /// Creates an independent deep clone of the captured arguments.
    /// </summary>
    /// <returns>A new <see cref="QueryArguments"/> instance.</returns>
    public QueryArguments CreateArguments()
    {
        var clone = new QueryArguments();
        foreach (var entry in _entries)
        {
            clone[entry.Key] = entry.CreateValue(_serializerOptions)!;
        }

        return clone;
    }

    sealed record Entry(string Key, Type? RuntimeType, byte[]? SerializedValue)
    {
        public static Entry Capture(string key, object? value, JsonSerializerOptions serializerOptions)
        {
            if (value is null)
            {
                return new Entry(key, null, null);
            }

            var runtimeType = value.GetType();
            return new Entry(key, runtimeType, JsonSerializer.SerializeToUtf8Bytes(value, runtimeType, serializerOptions));
        }

        public object? CreateValue(JsonSerializerOptions serializerOptions) =>
            RuntimeType is null
                ? null
                : JsonSerializer.Deserialize(SerializedValue, RuntimeType, serializerOptions);
    }
}
